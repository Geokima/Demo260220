using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net.Http;
using Cysharp.Threading.Tasks;
using Framework;
using UnityEngine;
using UnityEngine.Networking;
using static Framework.Logger;

namespace Framework.Modules.Http
{
    /// <summary>
    /// HTTP 系统实现类（Unity 环境）
    /// </summary>
    public class HttpSystem : AbstractSystem, IHttpSystem
    {
        #region Fields & Events

        /// <summary>
        /// 请求发送事件：方法、完整 URL、请求 JSON、响应内容、耗时(ms)、是否成功、状态码
        /// </summary>
        public static event Action<string, string, string, string, long, bool, int> OnRequestSent;

        /// <inheritdoc />
        public string ProdUrl { get; set; }
        /// <inheritdoc />
        public string TestUrl { get; set; }
        /// <inheritdoc />
        public bool IsTest { get; set; }

        /// <summary>
        /// 超时时间（秒）
        /// </summary>
        public int Timeout { get; set; } = 5;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public int MaxRetry { get; set; } = 3;

        /// <summary>
        /// 重试间隔（秒）
        /// </summary>
        public float RetryInterval { get; set; } = 1.0f;

        /// <inheritdoc />
        public string BaseUrl => IsTest ? TestUrl : ProdUrl;

        private readonly Dictionary<string, string> _defaultHeaders = new Dictionary<string, string>();
        private CancellationTokenSource _cts;

        #endregion

        #region Lifecycle

        /// <inheritdoc />
        public override void Init()
        {
            _cts = new CancellationTokenSource();
        }

        /// <inheritdoc />
        public override void Deinit()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _defaultHeaders.Clear();
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public void SetConfig(string prodUrl, string testUrl, bool isTest)
        {
            ProdUrl = prodUrl;
            TestUrl = testUrl;
            IsTest = isTest;
        }

        /// <inheritdoc />
        public void AddHeader(string key, string value)
        {
            _defaultHeaders[key] = value;
        }

        /// <summary>
        /// 获取 UnityWebRequest 对象（带参数回调）
        /// </summary>
        public void GetArgs(string url, Action<UnityWebRequest> callBack = null)
        {
            GetArgsAsync(url).ContinueWith(req => callBack?.Invoke(req)).Forget();
        }

        /// <inheritdoc />
        public void Get(string url, Action<string> callBack = null)
        {
            GetAsync(url).ContinueWith(txt => callBack?.Invoke(txt)).Forget();
        }

        /// <summary>
        /// 发起 POST 请求（带参数回调）
        /// </summary>
        public void PostArgs(string url, string json = null, Action<UnityWebRequest> callBack = null)
        {
            PostArgsAsync(url, json).ContinueWith(req => callBack?.Invoke(req)).Forget();
        }

        /// <inheritdoc />
        public void Post(string url, string json = null, Action<string> callBack = null)
        {
            PostAsync(url, json).ContinueWith(txt => callBack?.Invoke(txt)).Forget();
        }

        /// <summary>
        /// 异步获取 UnityWebRequest 对象
        /// </summary>
        public async UniTask<UnityWebRequest> GetArgsAsync(string url, CancellationToken token = default)
        {
            return await SendRequestAsync(url, UnityWebRequest.kHttpVerbGET, null, token);
        }

        /// <inheritdoc />
        public async UniTask<string> GetAsync(string url, CancellationToken token = default)
        {
            using var req = await GetArgsAsync(url, token);
            return req?.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null;
        }

        /// <summary>
        /// 异步发起 POST 请求并获取 UnityWebRequest 对象
        /// </summary>
        public async UniTask<UnityWebRequest> PostArgsAsync(string url, string json = null, CancellationToken token = default)
        {
            return await SendRequestAsync(url, UnityWebRequest.kHttpVerbPOST, json, token);
        }

        /// <inheritdoc />
        public async UniTask<string> PostAsync(string url, string json = null, CancellationToken token = default)
        {
            using var req = await PostArgsAsync(url, json, token);
            return req?.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null;
        }

        #endregion

        #region Private Methods

        private async UniTask<UnityWebRequest> SendRequestAsync(string url, string method, string json, CancellationToken token)
        {
            if (_cts == null) throw new OperationCanceledException("HttpSystem is not initialized");
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _cts.Token);

            // 发送开始事件，外部可监听此事件处理Loading UI
            this.SendEvent(new HttpStatusUpdateEvent { Url = url, IsLoading = true });

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            string fullUrl = url.StartsWith("http") ? url : $"{BaseUrl?.TrimEnd('/')}/{url?.TrimStart('/')}";

            int currentRetry = 0;
            while (true)
            {
                UnityWebRequest request = null;
                try
                {
                    request = CreateRequest(url, method, json);
                    await request.SendWebRequest().WithCancellation(linkedCts.Token);
                    stopwatch.Stop();

                    bool isSuccess = request.result == UnityWebRequest.Result.Success;
                    int statusCode = (int)request.responseCode;
                    string responseBody = request.downloadHandler?.text;

                    // 触发HTTP请求记录事件
                    OnRequestSent?.Invoke(method, fullUrl, json, responseBody, stopwatch.ElapsedMilliseconds, isSuccess, statusCode);

                    if (isSuccess)
                    {
                        this.SendEvent(new HttpStatusUpdateEvent { Url = url, IsLoading = false });
                        return request;
                    }

                    bool isError = request.result == UnityWebRequest.Result.ConnectionError || request.responseCode >= 500;
                    if (isError && currentRetry < MaxRetry && !linkedCts.IsCancellationRequested)
                    {
                        currentRetry++;
                        LogWarning($"[Http] Request failed, retrying ({currentRetry}/{MaxRetry}): {url}");
                        request.Dispose();
                        await UniTask.Delay(TimeSpan.FromSeconds(RetryInterval), cancellationToken: linkedCts.Token);
                        continue;
                    }

                    this.SendEvent(new HttpErrorEvent { Url = url, StatusCode = request.responseCode, Error = request.error });
                    this.SendEvent(new HttpStatusUpdateEvent { Url = url, IsLoading = false });
                    return request;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    request?.Dispose();

                    // 触发HTTP请求记录事件（异常）
                    OnRequestSent?.Invoke(method, fullUrl, json, ex.Message, stopwatch.ElapsedMilliseconds, false, 0);

                    if (currentRetry < MaxRetry && !linkedCts.IsCancellationRequested)
                    {
                        currentRetry++;
                        LogWarning($"[Http] Request exception, retrying ({currentRetry}/{MaxRetry}): {url}, Error: {ex.Message}");
                        await UniTask.Delay(TimeSpan.FromSeconds(RetryInterval), cancellationToken: linkedCts.Token);
                        continue;
                    }

                    this.SendEvent(new HttpStatusUpdateEvent { Url = url, IsLoading = false });
                    throw new HttpRequestException($"Request failed after {MaxRetry} retries, {ex.Message}");
                }
            }
        }

        private UnityWebRequest CreateRequest(string url, string method, string json)
        {
            if (string.IsNullOrEmpty(BaseUrl) && !url.StartsWith("http"))
                throw new OperationCanceledException("BaseUrl is not set");

            string fullUrl = url.StartsWith("http") ? url : $"{BaseUrl.TrimEnd('/')}/{url.TrimStart('/')}";
            var request = new UnityWebRequest(fullUrl, method);

            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = Timeout;

            if (!string.IsNullOrEmpty(json))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.SetRequestHeader("Content-Type", "application/json");
            }

            foreach (var header in _defaultHeaders)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }

            return request;
        }

        #endregion
    }
}
