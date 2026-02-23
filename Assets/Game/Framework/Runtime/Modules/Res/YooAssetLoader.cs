namespace Framework.Modules.Res
{
    using System.Collections;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    using YooAsset;
    using Object = UnityEngine.Object;

    public class YooAssetLoader : IResLoader
    {
        private readonly ResourcePackage _package;

        public YooAssetLoader(string packageName = "DefaultPackage")
        {
            _package = YooAssets.GetPackage(packageName);
            if (_package == null)
                Debug.LogError($"[Res] YooAsset package not found: {packageName}");
        }

        public T Load<T>(string path) where T : Object
        {
            return Load<T>(path, null);
        }

        public T Load<T>(string path, GameObject target) where T : Object
        {
            if (_package == null)
            {
                Debug.LogError("[Res] YooAsset package not initialized");
                return null;
            }

            var handle = _package.LoadAssetSync<T>(path);
            if (handle.Status == EOperationStatus.Succeed)
            {
                var asset = handle.AssetObject as T;
                BindHandle(handle, target);
                return asset;
            }
            else
            {
                Debug.LogError($"[Res] YooAsset load failed: {path}, status: {handle.Status}");
                handle.Release();
                return null;
            }
        }

        public async UniTask<T> LoadAsync<T>(string path) where T : Object
        {
            return await LoadAsync<T>(path, null);
        }

        public async UniTask<T> LoadAsync<T>(string path, GameObject target) where T : Object
        {
            if (_package == null)
            {
                Debug.LogError("[Res] YooAsset package not initialized");
                return null;
            }

            var handle = _package.LoadAssetAsync<T>(path);
            await handle;

            if (handle.Status == EOperationStatus.Succeed)
            {
                var asset = handle.AssetObject as T;
                BindHandle(handle, target);
                return asset;
            }
            else
            {
                Debug.LogError($"[Res] YooAsset load failed: {path}, status: {handle.Status}");
                handle.Release();
                return null;
            }
        }

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
                    Debug.LogError($"[Res] YooAsset unload failed: {ex.Message}");
                }
            }
        }



        private static GameObject _sceneRoot;

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
    }
}