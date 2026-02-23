using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Framework.Modules.Pool;

namespace Tests.PlayMode.Pool
{
    public class PoolSystemPlayModeTests
    {
        private PoolSystem _poolSystem;
        private GameObject _prefab;

        [SetUp]
        public void Setup()
        {
            _poolSystem = new PoolSystem();
            _poolSystem.Init(); // Will safely call GameObject.DontDestroyOnLoad in PlayMode

            _prefab = new GameObject("TestPrefab");
            _prefab.AddComponent<BoxCollider>();
        }

        [TearDown]
        public void Teardown()
        {
            _poolSystem.Deinit();
            if (_prefab != null) GameObject.Destroy(_prefab);
        }

        [UnityTest]
        public IEnumerator GameObjectPool_CreateAndRecycle_ShouldWorkInPlayMode()
        {
            _poolSystem.InitGameObjectPool(_prefab, isGlobal: false);
            var pool = _poolSystem.GetPool(_prefab, isGlobal: false);
            
            Assert.IsNotNull(pool, "GameObject pool should be created successfully.");
            
            // Allocate should instantiate our object
            GameObject obj1 = pool.Allocate();
            Assert.IsNotNull(obj1);
            Assert.IsTrue(obj1.activeSelf, "Allocated object should be active.");
            
            // Wait for one frame to simulate Unity lifecycle natively
            yield return null;

            // Recycle shouldn't destroy but deactivate and put back into the internal cache
            pool.Recycle(obj1);
            Assert.IsFalse(obj1.activeSelf, "Recycled object should be deactivated.");
            
            // Validate Pool Stats
            var stats = _poolSystem.GetPoolStats();
            Assert.IsTrue(stats.ContainsKey("TestPrefab"));
            Assert.AreEqual(1, stats["TestPrefab"].count, "Recycled object should enter the cache.");

            // Requesting again should hit the cache
            GameObject obj2 = pool.Allocate();
            Assert.AreSame(obj1, obj2, "Should return the exact same instance from cache.");
            
            yield return null;
        }
    }
}
