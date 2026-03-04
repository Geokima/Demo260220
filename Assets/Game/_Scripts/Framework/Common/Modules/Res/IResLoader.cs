using Cysharp.Threading.Tasks;

namespace Framework.Modules.Res
{
    /// <summary>
    /// 资源加载器接口
    /// </summary>
    public interface IResLoader
    {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <param name="target">生命周期绑定的目标 GameObject</param>
        /// <returns>资源对象</returns>
        T Load<T>(string path, UnityEngine.GameObject target = null) where T : class;

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <param name="target">生命周期绑定的目标 GameObject</param>
        /// <returns>加载任务</returns>
        UniTask<T> LoadAsync<T>(string path, UnityEngine.GameObject target = null) where T : class;

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <returns>是否存在</returns>
        bool Exists(string path);

        /// <summary>
        /// 卸载未使用的资源
        /// </summary>
        void UnloadUnusedAssets();
    }
}
