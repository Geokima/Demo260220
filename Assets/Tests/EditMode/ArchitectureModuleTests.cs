using NUnit.Framework;
using Framework;
using Framework.Modules.Config;
using Framework.Modules.Http;
using Framework.Modules.Res;
using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Tests.EditMode
{
    public class ArchitectureModuleTests
    {
        private IArchitecture _architecture;
        private ConfigSystem _configSystem;
        private HttpSystem _httpSystem;
        private ResSystem _resSystem;

        [SetUp]
        public void Setup()
        {
            _architecture = TestArchitecture.Instance;
            _configSystem = _architecture.GetSystem<ConfigSystem>();
            _httpSystem = _architecture.GetSystem<HttpSystem>();
            _resSystem = _architecture.GetSystem<ResSystem>();
        }

        [TearDown]
        public void Teardown()
        {
            if (_architecture != null)
            {
                _architecture.Shutdown();
            }
        }

        [Test]
        public void Architecture_ShouldInitializeAllModules()
        {
            Assert.IsNotNull(_configSystem, "ConfigSystem should be initialized");
            Assert.IsNotNull(_httpSystem, "HttpSystem should be initialized");
            Assert.IsNotNull(_resSystem, "ResSystem should be initialized");
        }

        [Test]
        public void ConfigSystem_ShouldLoadTestConfigsFromResources()
        {
            Assert.DoesNotThrow(() =>
            {
                // 使用ResSystem的ResourceLoader加载配置
                _configSystem.LoadConfigsFrom(_resSystem.ResourceLoader).AsTask().Wait();
                
                // 验证TestGameConfigRow配置是否加载成功
                var config = _configSystem.Get<TestGameConfigRow>(1);
                Assert.IsNotNull(config, "Should load TestGameConfigRow from cfg_testgame.json");
                Assert.AreEqual("Test Player", config.Name, "Should have correct name");
                Assert.AreEqual(100, config.MaxHealth, "Should have correct max health");
                
                var enemyConfig = _configSystem.Get<TestGameConfigRow>(2);
                Assert.IsNotNull(enemyConfig, "Should load enemy config");
                Assert.AreEqual("Test Enemy", enemyConfig.Name, "Should have correct enemy name");
                
                Debug.Log($"[Test] Loaded config: {config.Name}, HP: {config.MaxHealth}");
            }, "Should load configs from Resources without exception");
        }

        [Test]
        public void HttpSystem_ShouldBeConfigurable()
        {
            _httpSystem.SetConfig(
                prodUrl: "https://api.game.com",
                testUrl: "https://api.test.game.com",
                isTest: true
            );
            
            Assert.AreEqual("https://api.test.game.com", _httpSystem.BaseUrl, "Should use test URL");
            Assert.AreEqual(5, _httpSystem.Timeout, "Should have default timeout");
            Assert.AreEqual(3, _httpSystem.MaxRetry, "Should have default max retry");
            Assert.AreEqual(1.0f, _httpSystem.RetryInterval, "Should have default retry interval");
        }

        [Test]
        public void HttpSystem_ShouldSupportCustomConfiguration()
        {
            _httpSystem.Timeout = 10;
            _httpSystem.MaxRetry = 5;
            _httpSystem.RetryInterval = 2.5f;
            
            Assert.AreEqual(10, _httpSystem.Timeout, "Should set custom timeout");
            Assert.AreEqual(5, _httpSystem.MaxRetry, "Should set custom max retry");
            Assert.AreEqual(2.5f, _httpSystem.RetryInterval, "Should set custom retry interval");
        }

        [Test]
        public void HttpSystem_ShouldAddHeaders()
        {
            _httpSystem.AddHeader("Authorization", "Bearer test-token");
            _httpSystem.AddHeader("X-Client-Version", "1.0.0");
            
            // Headers are stored internally, just verify no exception
            Assert.Pass("Headers added without exception");
        }

        [Test]
        public void HttpSystem_ShouldHandleCancellation()
        {
            _httpSystem.SetConfig("https://httpbin.org", "https://httpbin.org", true);
            
            var cts = new CancellationTokenSource();
            cts.Cancel();
            
            Assert.Throws<OperationCanceledException>(() =>
            {
                _httpSystem.GetAsync("/test", cts.Token).AsTask().Wait();
            }, "Should throw when cancelled");
        }

        [Test]
        public void ResSystem_ShouldProvideResourceLoader()
        {
            var loader = _resSystem.ResourceLoader;
            Assert.IsNotNull(loader, "ResourceLoader should be available");
            
            // 测试加载一个资源（如果存在）
            Assert.DoesNotThrow(() =>
            {
                var asset = loader.Load<TextAsset>("test_simpleconfig");
                if (asset != null)
                {
                    Debug.Log($"[Test] Loaded test config: {asset.text.Length} chars");
                }
            }, "Should load resource without exception");
        }

        [Test]
        public void Modules_ShouldWorkTogether()
        {
            // 配置HttpSystem
            _httpSystem.SetConfig("https://httpbin.org", "https://httpbin.org", true);
            _httpSystem.AddHeader("X-Test", "UnityTest");
            
            // 加载配置
            Assert.DoesNotThrow(() =>
            {
                _configSystem.LoadConfigsFrom(_resSystem.ResourceLoader).AsTask().Wait();
            }, "Config loading should work");
            
            // 验证系统状态
            Assert.IsNotNull(_httpSystem.BaseUrl, "HttpSystem should be configured");
            Assert.IsNotNull(_resSystem.ResourceLoader, "ResSystem should have loader");
            
            Debug.Log("[Test] All modules working together");
        }

        [Test]
        public void Architecture_ShouldSupportEventSystem()
        {
            bool eventReceived = false;
            
            _architecture.RegisterEvent<TestEvent>(e =>
            {
                eventReceived = true;
                Assert.AreEqual("Test Message", e.Message);
            });
            
            _architecture.SendEvent(this, new TestEvent { Message = "Test Message" });
            
            Assert.IsTrue(eventReceived, "Should receive event");
        }

        public class TestEvent
        {
            public string Message { get; set; }
        }

        [Serializable]
        public class TestGameConfigRow : IConfigRow
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int MaxHealth { get; set; }
            public int AttackPower { get; set; }
            public int Defense { get; set; }
            
            int Framework.Modules.Config.IConfigRow.Id => Id;
        }

        private class TestArchitecture : Architecture<TestArchitecture>
        {
            protected override void RegisterModule()
            {
                RegisterSystem(new ConfigSystem());
                RegisterSystem(new HttpSystem());
                RegisterSystem(new ResSystem());
            }
            
        }
    }
}