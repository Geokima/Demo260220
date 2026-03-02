namespace Framework.Modules.Res
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using YooAsset;
    using static Framework.Logger;

    /// <summary>
    /// YooAsset 资源加载器实现
    /// </summary>
    public class YooAssetLoader : IResLoader
    {
        #region Fields

        private readonly ResourcePackage _package;
        private static GameObject _sceneRoot;

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="packageName">YooAsset 包名</param>
        public YooAssetLoader(string packageName = "DefaultPackage")
        {
            _package = YooAssets.GetPackage(packageName);
            if (_package == null)
                LogError($"[Res] YooAsset package not found: {packageName}");
        }

        #region Public Methods

        /// <inheritdoc />
        public T Load<T>(string path) where T : class
        {
            return Load<T>(path, null);
        }

        /// <summary>
        /// 同步加载资源并绑定生命周期
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <param name="target">生命周期绑定的目标 GameObject</param>
        /// <returns>资源对象</returns>
        public T Load<T>(string path, GameObject target) where T : class
        {
            if (_package == null)
            {
                LogError("[Res] YooAsset package not initialized");
                return null;
            }

            // 检查泛型 T 是否是 UnityEngine.Object
            if (!typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
            {
                LogError($"[Res] YooAsset only supports loading UnityEngine.Object. Type {typeof(T).Name} is invalid.");
                return null;
            }

            var handle = _package.LoadAssetSync(path, typeof(T));
            if (handle.Status == EOperationStatus.Succeed)
            {
                var asset = handle.AssetObject as T;
                BindHandle(handle, target);
                return asset;
            }
            else
            {
                LogError($"[Res] YooAsset load failed: {path}, status: {handle.Status}");
                handle.Release();
                return null;
            }
        }

        /// <inheritdoc />
        public async UniTask<T> LoadAsync<T>(string path) where T : class
        {
            return await LoadAsync<T>(path, null);
        }

        /// <summary>
        /// 异步加载资源并绑定生命周期
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <param name="target">生命周期绑定的目标 GameObject</param>
        /// <returns>加载任务</returns>
        public async UniTask<T> LoadAsync<T>(string path, GameObject target) where T : class
        {
            if (_package == null)
            {
                LogError("[Res] YooAsset package not initialized");
                return null;
            }

            // 检查泛型 T 是否是 UnityEngine.Object
            if (!typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
            {
                LogError($"[Res] YooAsset only supports loading UnityEngine.Object. Type {typeof(T).Name} is invalid.");
                return null;
            }

            var handle = _package.LoadAssetAsync(path, typeof(T));
            await handle;

            if (handle.Status == EOperationStatus.Succeed)
            {
                var asset = handle.AssetObject as T;
                BindHandle(handle, target);
                return asset;
            }
            else
            {
                LogError($"[Res] YooAsset load failed: {path}, status: {handle.Status}");
                handle.Release();
                return null;
            }
        }

        /// <inheritdoc />
        public bool Exists(string path)
        {
            if (_package == null)
            {
                LogError("[Res] YooAsset package not initialized");
                return false;
            }
            return _package.CheckLocationValid(path);
        }

        /// <inheritdoc />
        public void UnloadUnusedAssets()
        {
            if (_package != null)
            {
                try
                {
                    var operation = _package.UnloadUnusedAssetsAsync();
                    operation.WaitForAsyncComplete();
                }
                catch (System.Exception ex)
                {
                    LogError($"[Res] YooAsset unload failed: {ex.Message}");
                }
            }
        }

        #endregion

        #region Private Methods

        private void BindHandle(AssetHandle handle, GameObject target)
        {
            if (target == null)
            {
                if (_sceneRoot == null)
                    _sceneRoot = new GameObject("Res_Root");
                target = _sceneRoot;
            }

            var releaseHandle = target.GetComponent<AssetReleaseHandle>();
            if (releaseHandle == null)
                releaseHandle = target.AddComponent<AssetReleaseHandle>();  
            
            releaseHandle.AddHandle(handle);
        }

        private class AssetReleaseHandle : MonoBehaviour
        {
            private readonly List<AssetHandle> _releaseList = new List<AssetHandle>();

            public void AddHandle(AssetHandle handle)
            {
                _releaseList.Add(handle);
            }

            private void OnDestroy()
            {
                foreach (var handle in _releaseList)
                {
                    if (handle != null && handle.IsValid)
                        handle.Release();
                }
            }
        }

        #endregion
    }
}