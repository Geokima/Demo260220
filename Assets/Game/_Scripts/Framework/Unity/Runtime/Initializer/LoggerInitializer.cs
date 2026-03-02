using UnityEngine;
using Framework;

namespace Framework.Unity.Runtime.Initializer
{
    /// <summary>
    /// 日志工具初始化器
    /// </summary>
    internal static class LoggerInitializer
    {
        /// <summary>
        /// 在游戏启动前自动绑定 Unity 的日志输出
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            Framework.Logger.OnLog = (msg) => 
            {
                if (!Debug.isDebugBuild) return;
                Debug.Log(msg);
            };
            
            Framework.Logger.OnLogWarning = (msg) => Debug.LogWarning(msg);
            Framework.Logger.OnLogError = (msg) => Debug.LogError(msg);
        }
    }
}
