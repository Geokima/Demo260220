namespace Framework.Modules.Res
{
    using System;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public class ResSystem : AbstractSystem, IResSystem
    {
        #region Fields

        private IResLoader _loader;

        #endregion

        #region Lifecycle

        /// <inheritdoc />
        public override void Init()
        {
            // 目前默认使用 YooAssetLoader，未来可在此一键切换实现
            _loader = new YooAssetLoader();
            UnityEngine.Application.backgroundLoadingPriority = UnityEngine.ThreadPriority.High;
        }

        /// <inheritdoc />
        public override void Deinit()
        {
            _loader?.UnloadUnusedAssets();
            _loader = null;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public T Load<T>(string path) where T : class
        {
            return _loader?.Load<T>(path);
        }

        /// <inheritdoc />
        public async UniTask<T> LoadAsync<T>(string path) where T : class
        {
            if (_loader == null) return null;
            return await _loader.LoadAsync<T>(path);
        }

        /// <inheritdoc />
        public bool Exists(string path)
        {
            return _loader?.Exists(path) ?? false;
        }

        /// <inheritdoc />
        public void UnloadUnusedAssets()
        {
            _loader?.UnloadUnusedAssets();
        }

        #endregion
    }
}