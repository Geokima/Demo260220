using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Framework.Modules.Pool
{
    public class PoolSystem : AbstractSystem
    {
        private readonly Dictionary<Type, object> _typePools = new Dictionary<Type, object>();
        private readonly Dictionary<int, GameObjectPool> _globalGoPools = new Dictionary<int, GameObjectPool>();
        private readonly Dictionary<int, GameObjectPool> _sceneGoPools = new Dictionary<int, GameObjectPool>();
        private Transform _globalRoot;
        private Transform _sceneRoot;

        public override void Init()
        {
            _globalRoot = new GameObject("GlobalPool").transform;
        }

        private Transform GetSceneRoot()
        {
            if (_sceneRoot == null)
            {
                _sceneRoot = new GameObject("ScenePool").transform;
                _sceneGoPools.Clear();
            }
            return _sceneRoot;
        }

        public void InitGameObjectPool(GameObject prefab, bool isGlobal = false, Action<GameObject> onSpawn = null, Action<GameObject> onRecycle = null)
        {
            if (prefab == null) return;
            var dict = isGlobal ? _globalGoPools : _sceneGoPools;
            var id = prefab.GetInstanceID();
            if (dict.ContainsKey(id)) return;

            var root = isGlobal ? _globalRoot : GetSceneRoot();
            var pool = new GameObjectPool(prefab, root, onSpawn, onRecycle);
            pool.BindToken(root.gameObject.GetCancellationTokenOnDestroy());
            dict.Add(id, pool);
            Debug.Log($"[Pool] Created GameObjectPool for {prefab.name}");
        }

        public GameObjectPool GetPool(GameObject prefab, bool isGlobal = false)
        {
            if (prefab == null) return null;
            var dict = isGlobal ? _globalGoPools : _sceneGoPools;
            var id = prefab.GetInstanceID();
            if (dict.TryGetValue(id, out var pool)) return pool;

            var root = isGlobal ? _globalRoot : GetSceneRoot();
            var newPool = new GameObjectPool(prefab, root);
            newPool.BindToken(root.gameObject.GetCancellationTokenOnDestroy());
            dict.Add(id, newPool);
            Debug.Log($"[Pool] Created GameObjectPool for {prefab.name}");
            return newPool;
        }

        public Dictionary<string, (int count, string category)> GetPoolStats()
        {
            var stats = new Dictionary<string, (int count, string category)>();
            foreach (var kv in _typePools)
            {
                if (kv.Value is IPool pool) stats.Add(kv.Key.Name, (pool.Count, "CSharp"));
            }
            foreach (var kv in _globalGoPools)
                stats.Add($"Global_{kv.Key}", (kv.Value.Count, "Global_GO"));
            foreach (var kv in _sceneGoPools)
                stats.Add($"Scene_{kv.Key}", (kv.Value.Count, "Scene_GO"));
            return stats;
        }

        public SimpleObjectPool<T> GetPool<T>(Action<T> onReset = null) where T : new()
        {
            var type = typeof(T);
            if (_typePools.TryGetValue(type, out var pool)) return (SimpleObjectPool<T>)pool;
            var newPool = new SimpleObjectPool<T>(onReset);
            _typePools.Add(type, newPool);
            Debug.Log($"[Pool] Created SimpleObjectPool for {type.Name}");
            return newPool;
        }

        public override void Deinit()
        {
            _typePools.Clear();
            _globalGoPools.Clear();
            _sceneGoPools.Clear();
            if (_globalRoot != null) GameObject.Destroy(_globalRoot.gameObject);
            if (_sceneRoot != null) GameObject.Destroy(_sceneRoot.gameObject);
            _globalRoot = null;
            _sceneRoot = null;
        }
    }
}
