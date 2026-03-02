using UnityEngine;
using Framework;

namespace Framework.Unity.Runtime
{
    /// <summary>
    /// Unity 环境下的日志实现
    /// </summary>
    public class UnityLogger : AbstractSystem, ILogger
    {
        public void Log(string message) => Debug.Log(message);
        public void LogWarning(string message) => Debug.LogWarning(message);
        public void LogError(string message) => Debug.LogError(message);

        public override void Init() { }
        public override void Deinit() { }
    }
}
