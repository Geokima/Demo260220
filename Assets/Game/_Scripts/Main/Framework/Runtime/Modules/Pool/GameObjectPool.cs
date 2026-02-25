namespace Framework.Modules.Pool
{
    using System;
    using System.Threading;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public class GameObjectPool : Pool<GameObject>
    {
        private readonly GameObject _prefab;
        private readonly Transform _root;
        private readonly Action<GameObject> _onSpawn;
        private readonly Action<GameObject> _onRecycle;
        
        private int _maxReserve;
        private CancellationToken _token;
        private bool _isTaskRunning = false;

        public GameObjectPool(GameObject prefab, Transform root, Action<GameObject> onSpawn = null, Action<GameObject> onRecycle = null)
        {
            _prefab = prefab;
            _root = root;
            _onSpawn = onSpawn;
            _onRecycle = onRecycle;
        }

        public void SetMaxReserve(int max) => _maxReserve = max;
        public void BindToken(CancellationToken token) => _token = token;

        private async UniTaskVoid CleanupTaskAsync()
        {
            try
            {
                _isTaskRunning = true;
                while (Count > _maxReserve && !_token.IsCancellationRequested)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(5), cancellationToken: _token);

                    int trimAmount = Mathf.Max((Count - _maxReserve) / 2, 1);
                    for (int i = 0; i < trimAmount; i++)
                    {
                        if (_token.IsCancellationRequested || Count <= _maxReserve) break;

                        InternalPopAndDestroy();
                        await UniTask.Yield(PlayerLoopTiming.Update, _token);
                    }
                }
            }
            finally { _isTaskRunning = false; }
        }

        protected override GameObject Create()
        {
            if (_prefab == null)
            {
                Debug.LogError("[GameObjectPool] Prefab is null, cannot create object");
                return null;
            }
            var obj = UnityEngine.Object.Instantiate(_prefab, _root);
            obj.name = $"{_prefab.name} [Pooled]";
            return obj;
        }

        public override GameObject Allocate()
        {
            GameObject obj = base.Allocate();
            if (obj == null) return null;

            obj.SetActive(true);
            var components = obj.GetComponentsInChildren<IPoolObject>(true);
            foreach (var c in components) c.OnSpawn();
            _onSpawn?.Invoke(obj);
            return obj;
        }

        public override bool Recycle(GameObject obj)
        {
            if (!base.Recycle(obj)) return false;

            var components = obj.GetComponentsInChildren<IPoolObject>(true);
            foreach (var c in components) c.OnDespawn();
            _onRecycle?.Invoke(obj);

            obj.SetActive(false);
            obj.transform.SetParent(_root);

            if (Count > _maxReserve && !_isTaskRunning) CleanupTaskAsync().Forget();
            return true;
        }

        public override void Clear()
        {
            while (_cache.Count > 0)
            {
                var item = _cache.Pop();
                if (item != null) UnityEngine.Object.Destroy(item);
            }
            _inPoolCheck.Clear();
        }

        private void InternalPopAndDestroy()
        {
            if (_cache.Count > 0)
            {
                var obj = _cache.Pop();
                _inPoolCheck.Remove(obj);
                if (obj != null) UnityEngine.Object.Destroy(obj);
            }
        }
    }
}
