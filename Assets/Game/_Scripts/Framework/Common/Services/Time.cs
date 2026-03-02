using System;

namespace Framework
{
    /// <summary>
    /// 跨平台时间工具类
    /// </summary>
    public static class Time
    {
        #region 桥接委托
        
        public static Func<float> GetTime;
        public static Func<float> GetDeltaTime;
        public static Func<float> GetRealtimeSinceStartup;
        
        #endregion

        #region 公开属性
        
        /// <summary>
        /// 获取当前游戏时间（秒）
        /// </summary>
        public static float Now => GetTime?.Invoke() ?? 0f;

        /// <summary>
        /// 获取上一帧的增量时间（秒）
        /// </summary>
        public static float DeltaTime => GetDeltaTime?.Invoke() ?? 0f;

        /// <summary>
        /// 获取自游戏启动以来的实时时间（秒）
        /// </summary>
        public static float RealtimeSinceStartup => GetRealtimeSinceStartup?.Invoke() ?? 0f;
        
        #endregion
    }
}
