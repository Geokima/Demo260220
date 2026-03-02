using UnityEngine;
using Framework;

namespace Framework.Unity.Runtime.Initializer
{
    /// <summary>
    /// 时间工具初始化器
    /// </summary>
    internal static class TimeInitializer
    {
        /// <summary>
        /// 在游戏启动前自动绑定 Unity 的时间获取
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            Framework.Time.GetTime = () => UnityEngine.Time.time;
            Framework.Time.GetDeltaTime = () => UnityEngine.Time.deltaTime;
            Framework.Time.GetRealtimeSinceStartup = () => UnityEngine.Time.realtimeSinceStartup;
        }
    }
}
