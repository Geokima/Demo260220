namespace Framework.Modules.Res
{
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public class ResourcesLoader : IResLoader
    {
        public T Load<T>(string path) where T : Object
        {
            return Resources.Load<T>(path);
        }

        public async UniTask<T> LoadAsync<T>(string path) where T : UnityEngine.Object
        {
            var request = Resources.LoadAsync<T>(path);
            await request;
            return request.asset as T;
        }

        public void UnloadUnusedAssets()
        {
            Resources.UnloadUnusedAssets();
        }
    }
}
