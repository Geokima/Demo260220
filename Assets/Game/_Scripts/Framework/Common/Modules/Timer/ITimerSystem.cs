using System;
using Framework;

namespace Framework.Modules.Timer
{
    /// <summary>
    /// 计时器系统接口
    /// </summary>
    public interface ITimerSystem : ISystem
    {
        /// <summary>
        /// 添加一次性计时器
        /// </summary>
        /// <param name="delay">延迟时间（秒）</param>
        /// <param name="callback">回调函数</param>
        /// <returns>计时器 ID</returns>
        int AddTimer(float delay, Action callback);

        /// <summary>
        /// 添加循环计时器
        /// </summary>
        /// <param name="delay">延迟时间（秒）</param>
        /// <param name="loop">循环次数，-1 表示无限循环</param>
        /// <param name="callback">回调函数</param>
        /// <returns>计时器 ID</returns>
        int AddTimer(float delay, int loop, Action callback);

        /// <summary>
        /// 移除计时器
        /// </summary>
        /// <param name="timerId">计时器 ID</param>
        void RemoveTimer(int timerId);
    }
}
