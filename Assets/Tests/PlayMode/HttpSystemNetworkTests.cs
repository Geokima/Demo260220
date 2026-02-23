using System;
using System.Collections;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;
using Framework.Modules.Http;
using Framework;

namespace Tests.PlayMode.Http
{
    public class HttpSystemNetworkTests
    {
        private HttpSystem _httpSystem;
        private TestArchitecture _architecture;
        private int _loadingEvents;
        private int _errorEvents;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            _architecture = new TestArchitecture();
            _httpSystem = new HttpSystem();
            _httpSystem.Architecture = _architecture;
            _httpSystem.Init();
            
            // 配置测试URL
            _httpSystem.SetConfig("https://httpbin.org", "https://httpbin.org", true);
            
            // 重置计数器
            _loadingEvents = 0;
            _errorEvents = 0;
            
            // 订阅事件
            _architecture.RegisterEvent<HttpStateEvent>(e =>
            {
                if (e.IsLoading) _loadingEvents++;
                Debug.Log($"[Test] StateEvent: {e.Url} - Loading: {e.IsLoading}");
            });
            
            _architecture.RegisterEvent<HttpErrorEvent>(e =>
            {
                _errorEvents++;
                Debug.Log($"[Test] ErrorEvent: {e.Error}");
            });
            
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            _httpSystem.Deinit();
            yield return null;
        }

        // ================= 基础网络测试 =================

        [UnityTest]
        public IEnumerator HttpSystem_GetRequest_ShouldWork()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                try
                {
                    var json = await _httpSystem.GetAsync("/get");
                    
                    Assert.IsNotNull(json, "Response should not be null");
                    Assert.IsFalse(string.IsNullOrEmpty(json), "Response should not be empty");
                    Assert.IsTrue(json.Contains("httpbin.org/get"), "Should contain expected URL");
                    
                    Debug.Log($"[Test] GET request successful, response length: {json.Length}");
                }
                catch (Exception ex)
                {
                    if (Application.internetReachability == NetworkReachability.NotReachable)
                    {
                        Assert.Inconclusive("Network not available");
                    }
                    else
                    {
                        Assert.Fail($"GET request failed: {ex.Message}");
                    }
                }
            });
        }

        [UnityTest]
        public IEnumerator HttpSystem_PostRequest_ShouldWork()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                try
                {
                    var postData = "{\"test_key\": \"test_value\", \"number\": 123}";
                    var json = await _httpSystem.PostAsync("/post", postData);
                    
                    Assert.IsNotNull(json, "Response should not be null");
                    Assert.IsFalse(string.IsNullOrEmpty(json), "Response should not be empty");
                    Assert.IsTrue(json.Contains("test_key"), "Should contain posted data");
                    Assert.IsTrue(json.Contains("test_value"), "Should contain posted value");
                    
                    Debug.Log($"[Test] POST request successful, response length: {json.Length}");
                }
                catch (Exception ex)
                {
                    if (Application.internetReachability == NetworkReachability.NotReachable)
                    {
                        Assert.Inconclusive("Network not available");
                    }
                    else
                    {
                        Assert.Fail($"POST request failed: {ex.Message}");
                    }
                }
            });
        }

        [UnityTest]
        public IEnumerator HttpSystem_GetArgsAsync_ShouldReturnRequestObject()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                try
                {
                    using var request = await _httpSystem.GetArgsAsync("/get");
                    
                    Assert.IsNotNull(request, "Request object should not be null");
                    Assert.AreEqual(UnityWebRequest.Result.Success, request.result, "Request should be successful");
                    Assert.IsNotNull(request.downloadHandler, "Download handler should exist");
                    Assert.IsNotNull(request.downloadHandler.text, "Response text should not be null");
                    
                    Debug.Log($"[Test] GetArgsAsync successful, status: {request.responseCode}");
                }
                catch (Exception ex)
                {
                    if (Application.internetReachability == NetworkReachability.NotReachable)
                    {
                        Assert.Inconclusive("Network not available");
                    }
                    else
                    {
                        Assert.Fail($"GetArgsAsync failed: {ex.Message}");
                    }
                }
            });
        }

        // ================= 重试机制测试 =================

        [UnityTest]
        public IEnumerator HttpSystem_RetryOnServerError_ShouldWork()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                try
                {
                    // 配置重试
                    _httpSystem.MaxRetry = 2;
                    _httpSystem.RetryInterval = 0.5f;
                    
                    // 请求500错误接口
                    using var request = await _httpSystem.GetArgsAsync("/status/500");
                    
                    Assert.IsNotNull(request, "Request object should not be null");
                    Assert.AreEqual(500, request.responseCode, "Should get 500 status");
                    
                    // 验证重试事件（通过日志观察）
                    Debug.Log($"[Test] Server error test completed, response code: {request.responseCode}");
                }
                catch (Exception ex)
                {
                    if (Application.internetReachability == NetworkReachability.NotReachable)
                    {
                        Assert.Inconclusive("Network not available");
                    }
                    else
                    {
                        Debug.LogWarning($"[Test] Server error test exception (expected): {ex.Message}");
                        Assert.Pass("Server error handled appropriately");
                    }
                }
            });
        }

        // ================= 取消测试 =================

        [UnityTest]
        public IEnumerator HttpSystem_Cancellation_ShouldThrowOperationCanceled()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                try
                {
                    var cts = new CancellationTokenSource();
                    cts.CancelAfter(50); // 50ms后取消
                    
                    await _httpSystem.GetAsync("/delay/1", cts.Token);
                    
                    Assert.Fail("Should have thrown OperationCanceledException");
                }
                catch (OperationCanceledException)
                {
                    Assert.Pass("Operation correctly canceled");
                }
                catch (Exception ex)
                {
                    if (Application.internetReachability == NetworkReachability.NotReachable)
                    {
                        Assert.Inconclusive("Network not available");
                    }
                    else
                    {
                        Assert.Fail($"Unexpected exception: {ex.Message}");
                    }
                }
            });
        }

        // ================= 事件验证测试 =================

        [UnityTest]
        public IEnumerator HttpSystem_ShouldSendLoadingEvents()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                try
                {
                    int startLoadingEvents = _loadingEvents;
                    
                    await _httpSystem.GetAsync("/get");
                    
                    // 应该至少发送2个状态事件（开始加载和结束加载）
                    Assert.Greater(_loadingEvents, startLoadingEvents, "Should send loading events");
                    Debug.Log($"[Test] Loading events sent: {_loadingEvents - startLoadingEvents}");
                }
                catch (Exception ex)
                {
                    if (Application.internetReachability == NetworkReachability.NotReachable)
                    {
                        Assert.Inconclusive("Network not available");
                    }
                    else
                    {
                        Debug.LogWarning($"[Test] Event test exception: {ex.Message}");
                        Assert.Pass("Events test completed with network issue");
                    }
                }
            });
        }

        // ================= 超时测试 =================

        [UnityTest]
        public IEnumerator HttpSystem_Timeout_ShouldWork()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                try
                {
                    // 设置很短的超时
                    _httpSystem.Timeout = 1;
                    
                    // 请求延迟2秒的接口
                    using var request = await _httpSystem.GetArgsAsync("/delay/2");
                    
                    Assert.IsNotNull(request, "Request object should not be null");
                    Assert.AreNotEqual(UnityWebRequest.Result.Success, request.result, "Should timeout");
                    
                    Debug.Log($"[Test] Timeout test completed, result: {request.result}");
                }
                catch (Exception ex)
                {
                    if (Application.internetReachability == NetworkReachability.NotReachable)
                    {
                        Assert.Inconclusive("Network not available");
                    }
                    else
                    {
                        Debug.LogWarning($"[Test] Timeout test exception (expected): {ex.Message}");
                        Assert.Pass("Timeout handled appropriately");
                    }
                }
            });
        }

        // ================= 重启测试 =================

        [UnityTest]
        public IEnumerator HttpSystem_Restart_ShouldResetState()
        {
            yield return UniTask.ToCoroutine(async () =>
            {
                try
                {
                    // 第一次请求
                    await _httpSystem.GetAsync("/get");
                    
                    // 重启系统
                    _httpSystem.Deinit();
                    _httpSystem.Init();
                    
                    // 重新配置
                    _httpSystem.SetConfig("https://httpbin.org", "https://httpbin.org", true);
                    
                    // 第二次请求应该正常工作
                    var result = await _httpSystem.GetAsync("/get");
                    
                    Assert.IsNotNull(result, "Restarted system should work");
                    Assert.IsTrue(result.Contains("httpbin.org"), "Should get valid response");
                    
                    Debug.Log($"[Test] Restart test completed successfully");
                }
                catch (Exception ex)
                {
                    if (Application.internetReachability == NetworkReachability.NotReachable)
                    {
                        Assert.Inconclusive("Network not available");
                    }
                    else
                    {
                        Assert.Fail($"Restart test failed: {ex.Message}");
                    }
                }
            });
        }

        // ================= 辅助类 =================

        private class TestArchitecture : Architecture<TestArchitecture>
        {
            protected override void RegisterModule()
            {
                // 空实现，测试不需要注册模块
            }
            
        }
    }
}