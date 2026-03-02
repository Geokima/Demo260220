using System;

namespace Framework
{
    /// <summary>
    /// 静态日志工具类
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// 外部注入的日志输出委托
        /// </summary>
        public static Action<string> OnLog;
        
        /// <summary>
        /// 外部注入的警告输出委托
        /// </summary>
        public static Action<string> OnLogWarning;
        
        /// <summary>
        /// 外部注入的错误输出委托
        /// </summary>
        public static Action<string> OnLogError;

        /// <summary>
        /// 打印普通日志
        /// </summary>
        public static void Log(object message) 
        {
#if DEBUG_LOG_NORMAL
            OnLog?.Invoke(message?.ToString());
#endif
        }

        /// <summary>
        /// 打印警告日志
        /// </summary>
        public static void LogWarning(object message) 
        {
#if DEBUG_LOG_WARNING
            OnLogWarning?.Invoke(message?.ToString());
#endif
        }

        /// <summary>
        /// 打印错误日志
        /// </summary>
        public static void LogError(object message) 
        {
#if DEBUG_LOG_ERROR
            OnLogError?.Invoke(message?.ToString());
#endif
        }
    }
}
