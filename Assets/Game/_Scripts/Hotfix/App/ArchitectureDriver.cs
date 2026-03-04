using Game;
using UnityEngine;

namespace Game
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
    }
}
