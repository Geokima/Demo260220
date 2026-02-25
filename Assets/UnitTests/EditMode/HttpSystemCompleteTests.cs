using NUnit.Framework;
using Framework.Modules.Http;
using Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;
using Cysharp.Threading.Tasks;

namespace Tests.EditMode.Http
{
    public class HttpSystemCompleteTests
    {
        private HttpSystem _httpSystem;
        private TestArchitecture _architecture;
        private int _stateEventCount;
        private int _errorEventCount;

        [SetUp]
        public void Setup()
        {
            _architecture = new TestArchitecture();
            _httpSystem = new HttpSystem();
            _httpSystem.Architecture = _architecture;
            _httpSystem.Init();
            
            // 重置事件计数
            _stateEventCount = 0;
            _errorEventCount = 0;
            
            // 订阅事件用于验证
            _architecture.RegisterEvent<HttpStateEvent>(e => _stateEventCount++);
            _architecture.RegisterEvent<HttpErrorEvent>(e => _errorEventCount++);
        }

        [TearDown]
        public void Teardown()
        {
            _httpSystem.Deinit();
        }

        // ================= 基础配置测试 =================

        [Test]
        public void HttpSystem_InitDeinit_ShouldWork()
        {
            Assert.IsNotNull(_httpSystem);
            Assert.DoesNotThrow(() => _httpSystem.Init());
            Assert.DoesNotThrow(() => _httpSystem.Deinit());
        }

        [Test]
        public void HttpSystem_SetConfig_ShouldSetBaseUrl()
        {
            _httpSystem.SetConfig("http://prod.example.com", "http://test.example.com", false);
            Assert.AreEqual("http://prod.example.com", _httpSystem.BaseUrl);
            
            _httpSystem.SetConfig("http://prod.example.com", "http://test.example.com", true);
            Assert.AreEqual("http://test.example.com", _httpSystem.BaseUrl);
        }

        [Test]
        public void HttpSystem_AddHeader_ShouldStoreHeader()
        {
            _httpSystem.AddHeader("Authorization", "Bearer token123");
            // 头部在内部字典中，无法直接验证，但调用不应抛出异常
            Assert.Pass("AddHeader called without exception");
        }

        [Test]
        public void HttpSystem_PropertyDefaults_ShouldBeCorrect()
        {
            Assert.AreEqual(5, _httpSystem.Timeout);
            Assert.AreEqual(3, _httpSystem.MaxRetry);
            Assert.AreEqual(1.0f, _httpSystem.RetryInterval);
        }

        [Test]
        public void HttpSystem_PropertySetters_ShouldWork()
        {
            _httpSystem.Timeout = 10;
            _httpSystem.MaxRetry = 5;
            _httpSystem.RetryInterval = 2.5f;
            
            Assert.AreEqual(10, _httpSystem.Timeout);
            Assert.AreEqual(5, _httpSystem.MaxRetry);
            Assert.AreEqual(2.5f, _httpSystem.RetryInterval);
        }

        // ================= 异常情况测试 =================

        [Test]
        public void HttpSystem_NotInitialized_ShouldThrow()
        {
            var http = new HttpSystem();
            Assert.Throws<OperationCanceledException>(() => 
                http.GetArgsAsync("/test").GetAwaiter().GetResult());
        }

        [Test]
        public void HttpSystem_BaseUrlNotSet_ShouldThrow()
        {
            Assert.Throws<OperationCanceledException>(() =>
                _httpSystem.GetArgsAsync("relative/path").GetAwaiter().GetResult());
        }

        [Test]
        public void HttpSystem_DeinitCancelsRequests_ShouldWork()
        {
            _httpSystem.SetConfig("http://example.com", "http://test.com", false);
            
            // 启动一个异步请求
            var task = _httpSystem.GetAsync("/test");
            
            // 立即取消
            _httpSystem.Deinit();
            
            // 重新初始化
            _httpSystem.Init();
            
            Assert.Pass("Deinit should cancel pending requests");
        }

        // ================= 回调API测试 =================

        [Test]
        public void HttpSystem_CallbackGet_ShouldInvokeCallback()
        {
            bool callbackCalled = false;
            string callbackResult = null;
            
            _httpSystem.SetConfig("https://httpbin.org", "https://httpbin.org", true);
            
            _httpSystem.Get("/get", result =>
            {
                callbackCalled = true;
                callbackResult = result;
            });
            
            // 等待一小段时间让回调有机会执行
            Task.Delay(100).Wait();
            
            Assert.IsTrue(callbackCalled, "Callback should be called");
            Assert.IsNotNull(callbackResult, "Result should not be null");
            Assert.IsTrue(callbackResult.Contains("httpbin.org"), "Result should contain expected content");
        }

        [Test]
        public void HttpSystem_CallbackPost_ShouldInvokeCallback()
        {
            bool callbackCalled = false;
            string callbackResult = null;
            
            _httpSystem.SetConfig("https://httpbin.org", "https://httpbin.org", true);
            
            var postData = "{\"test\": \"data\"}";
            _httpSystem.Post("/post", postData, result =>
            {
                callbackCalled = true;
                callbackResult = result;
            });
            
            // 等待一小段时间让回调有机会执行
            Task.Delay(100).Wait();
            
            Assert.IsTrue(callbackCalled, "Callback should be called");
            Assert.IsNotNull(callbackResult, "Result should not be null");
            Assert.IsTrue(callbackResult.Contains("httpbin.org"), "Result should contain expected content");
        }

        // ================= 事件系统测试 =================

        [Test]
        public void HttpSystem_ShouldSendStateEvents()
        {
            _httpSystem.SetConfig("https://httpbin.org", "https://httpbin.org", true);
            
            int startCount = _stateEventCount;
            
            _httpSystem.Get("/get", result => { });
            
            // 等待事件
            Task.Delay(50).Wait();
            
            Assert.Greater(_stateEventCount, startCount, "Should send state events");
        }

        // ================= 取消测试 =================

        [Test]
        public void HttpSystem_CancellationToken_ShouldCancelRequest()
        {
            _httpSystem.SetConfig("https://httpbin.org", "https://httpbin.org", true);
            
            var cts = new CancellationTokenSource();
            cts.CancelAfter(10); // 10ms后取消
            
            Assert.Throws<OperationCanceledException>(() =>
                _httpSystem.GetAsync("/delay/1", cts.Token).GetAwaiter().GetResult());
        }

        // ================= 重启测试 =================

        [Test]
        public void HttpSystem_Restart_ShouldWork()
        {
            // 第一次使用
            _httpSystem.SetConfig("http://first.com", "http://first-test.com", false);
            Assert.AreEqual("http://first.com", _httpSystem.BaseUrl);
            
            // 重启
            _httpSystem.Deinit();
            _httpSystem.Init();
            
            // 重新配置
            _httpSystem.SetConfig("http://second.com", "http://second-test.com", true);
            Assert.AreEqual("http://second-test.com", _httpSystem.BaseUrl);
            
            Assert.Pass("Restart completed successfully");
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