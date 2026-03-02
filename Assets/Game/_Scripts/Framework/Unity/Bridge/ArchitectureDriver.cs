using Game;
using UnityEngine;

namespace Framework.Unity.Bridge
{
    /// <summary>
    /// 架构驱动器 (粘合层)
    /// 负责将 Unity 生命周期事件驱动到框架核心逻辑中
    /// </summary>
    [DefaultExecutionOrder(-100)] // 确保在业务逻辑之前运行
    public class ArchitectureDriver : MonoBehaviour
    {
        private void Update()
        {
            if (GameArchitecture.Instance != null)
            {
                GameArchitecture.Instance.Update();
            }
        }

        private void FixedUpdate()
        {
            if (GameArchitecture.Instance != null)
            {
                GameArchitecture.Instance.FixedUpdate();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                // TODO: 考虑通过接口统一分发 Resume 事件
                // 目前由 GameManager 直接分发给特定系统
            }
        }
    }
}
