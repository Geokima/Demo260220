using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Framework;
using UnityEngine;
using UnityEngine.Networking;

namespace Framework.Modules.Http
{
    public class HttpSystem : AbstractSystem
    {
        public static event Action<string, string, string, string, long, bool, int> OnRequestSent;
        
        public string ProdUrl { get; set; }
        public string TestUrl { get; set; }
        public bool IsTest { get; set; }

        public int Timeout { get; set; } = 5;
        public int MaxRetry { get; set; } = 3;
        public float RetryInterval { get; set; } = 1.0f;

        public string BaseUrl => IsTest ? TestUrl : ProdUrl;

        private readonly Dictionary<string, string> _defaultHeaders = new Dictionary<string, string>();
        private CancellationTokenSource _cts;

        public override void Init()
        {
            _cts = new CancellationTokenSource();
        }

        public override void Deinit()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _defaultHeaders.Clear();
        }

        public void SetConfig(string prodUrl, string testUrl, bool isTest)
        {
            ProdUrl = prodUrl;
            TestUrl = testUrl;
            IsTest = isTest;
        }

        public void AddHeader(string key, string value)
        {
            _defaultHeaders[key] = value;
        }

        public void GetArgs(string url, Action<UnityWebRequest> callBack = null)
        {
            GetArgsAsync(url).ContinueWith(req => callBack?.Invoke(req)).Forget();
        }

        public void Get(string url, Action<string> callBack = null)
        {
            GetAsync(url).ContinueWith(txt => callBack?.Invoke(txt)).Forget();
        }

        public void PostArgs(string url, string json = null, Action<UnityWebRequest> callBack = null)
        {
            PostArgsAsync(url, json).ContinueWith(req => callBack?.Invoke(req)).Forget();
        }

        public void Post(string url, string json = null, Action<string> callBack = null)
        {
            PostAsync(url, json).ContinueWith(txt => callBack?.Invoke(txt)).Forget();
        }

        public async UniTask<UnityWebRequest> GetArgsAsync(string url, CancellationToken token = default)
        {
            return await SendRequestAsync(url, UnityWebRequest.kHttpVerbGET, null, token);
        }

        public async UniTask<string> GetAsync(string url, CancellationToken token = default)
        {
            using var req = await GetArgsAsync(url, token);
            return req?.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null;
        }

        public async UniTask<UnityWebRequest> PostArgsAsync(string url, string json = null, CancellationToken token = default)
        {
            return await SendRequestAsync(url, UnityWebRequest.kHttpVerbPOST, json, token);
        }

        public async UniTask<string> PostAsync(string url, string json = null, CancellationToken token = default)
        {
            using var req = await PostArgsAsync(url, json, token);
            return req?.result == UnityWebRequest.Result.Success ? req.downloadHandler.text : null;
        }

        private async UniTask<UnityWebRequest> SendRequestAsync(string url, string method, string json, CancellationToken token)
        {
            if (_cts == null) throw new OperationCanceledException("HttpSystem is not initialized");
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _cts.Token);

            // 发送开始事件，外部可监听此事件处理Loading UI
            this.SendEvent(new HttpStateEvent { Url = url, IsLoading = true });
            
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
                        this.SendEvent(new HttpStateEvent { Url = url, IsLoading = false });
                        return request;
                    }

                    bool isError = request.result == UnityWebRequest.Result.ConnectionError || request.responseCode >= 500;
                    if (isError && currentRetry < MaxRetry && !linkedCts.IsCancellationRequested)
                    {
                        currentRetry++;
                        Debug.LogWarning($"[Http] Request failed, retrying ({currentRetry}/{MaxRetry}): {url}");
                        request.Dispose();
                        await UniTask.Delay(TimeSpan.FromSeconds(RetryInterval), cancellationToken: linkedCts.Token);
                        continue;
                    }

                    this.SendEvent(new HttpErrorEvent { Request = request, Error = request.error });
                    this.SendEvent(new HttpStateEvent { Url = url, IsLoading = false });
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
                        Debug.LogWarning($"[Http] Request exception, retrying ({currentRetry}/{MaxRetry}): {url}, Error: {ex.Message}");
                        await UniTask.Delay(TimeSpan.FromSeconds(RetryInterval), cancellationToken: linkedCts.Token);
                        continue;
                    }
                    
                    this.SendEvent(new HttpStateEvent { Url = url, IsLoading = false });
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
    }
}
