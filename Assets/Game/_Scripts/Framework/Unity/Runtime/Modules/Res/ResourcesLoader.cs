namespace Framework.Modules.Res
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    /// <summary>
    /// Unity Resources 加载器实现
    /// </summary>
    public class ResourcesLoader : IResLoader
    {
        /// <inheritdoc />
        public T Load<T>(string path) where T : class
        {
            if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
            {
                return Resources.Load(path, typeof(T)) as T;
            }
            return null;
        }

        /// <inheritdoc />
        public async UniTask<T> LoadAsync<T>(string path) where T : class
        {
            if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
            {
                var request = Resources.LoadAsync(path, typeof(T));
                await request;
                return request.asset as T;
            }
            return null;
        }

        /// <inheritdoc />
        public bool Exists(string path)
        {
            var asset = Resources.Load(path);
            if (asset != null)
            {
                Resources.UnloadAsset(asset);
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public void UnloadUnusedAssets()
        {
            Resources.UnloadUnusedAssets();
        }
    }
}
