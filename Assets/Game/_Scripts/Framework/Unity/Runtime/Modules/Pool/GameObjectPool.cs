using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static Framework.Logger;

namespace Framework.Modules.Pool
{
    /// <summary>
    /// GameObject 专用对象池
    /// </summary>
    public class GameObjectPool : Pool<GameObject>
    {
        #region Fields

        private readonly GameObject _prefab;
        private readonly Transform _root;
        private readonly Action<GameObject> _onSpawn;
        private readonly Action<GameObject> _onRecycle;
        
        private int _maxReserve;
        private CancellationToken _token;
        private bool _isTaskRunning = false;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="prefab">原始预制体</param>
        /// <param name="root">池节点的父级变换</param>
        /// <param name="onSpawn">生成时的额外回调</param>
        /// <param name="onRecycle">回收时的额外回调</param>
        public GameObjectPool(GameObject prefab, Transform root, Action<GameObject> onSpawn = null, Action<GameObject> onRecycle = null)
        {
            _prefab = prefab;
            _root = root;
            _onSpawn = onSpawn;
            _onRecycle = onRecycle;
        }

        #region Public Methods

        /// <summary>
        /// 设置最大保留数量
        /// </summary>
        public void SetMaxReserve(int max) => _maxReserve = max;

        /// <summary>
        /// 绑定生命周期令牌（通常是根节点的 OnDestroy 令牌）
        /// </summary>
        public void BindToken(CancellationToken token) => _token = token;

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <summary>
        /// 清空对象池并销毁所有缓存实例
        /// </summary>
        public override void Clear()
        {
            while (_cache.Count > 0)
            {
                var item = _cache.Pop();
                if (item != null) UnityEngine.Object.Destroy(item);
            }
            _inPoolCheck.Clear();
        }

        #endregion

        #region Private Methods

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

        /// <inheritdoc />
        protected override GameObject Create()
        {
            if (_prefab == null)
            {
                LogError("[GameObjectPool] Prefab is null, cannot create object");
                return null;
            }
            var obj = UnityEngine.Object.Instantiate(_prefab, _root);
            obj.name = $"{_prefab.name} [Pooled]";
            return obj;
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

        #endregion
    }
}
