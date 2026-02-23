namespace Framework.Modules.Res
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public interface IResLoader
    {
        T Load<T>(string path) where T : UnityEngine.Object;
        UniTask<T> LoadAsync<T>(string path) where T : UnityEngine.Object;
        void UnloadUnusedAssets();
    }
}
