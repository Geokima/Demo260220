using System.Collections.Generic;
using Framework;

namespace Framework.Modules.Scene
{
    /// <summary>
    /// 场景系统接口
    /// </summary>
    public interface ISceneSystem : ISystem
    {
        /// <summary>
        /// 当前已加载的场景列表
        /// </summary>
        List<string> CurrentScenes { get; }

        /// <summary>
        /// 加载单个场景（先卸载当前所有场景）
        /// </summary>
        /// <param name="scenePath">场景资源路径</param>
        void LoadScene(string scenePath);

        /// <summary>
        /// 同时加载多个场景
        /// </summary>
        /// <param name="scenePaths">场景资源路径数组</param>
        void LoadScenes(string[] scenePaths);

        /// <summary>
        /// 获取当前场景加载的总进度
        /// </summary>
        /// <returns>进度值 (0-1)</returns>
        float GetSceneLoadProgress();
    }
}
