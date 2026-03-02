using System;

namespace Framework
{
    /// <summary>
    /// 静态日志工具类
    /// </summary>
    public static class Logger
    {
        public static Action<string> OnLog;
        public static Action<string> OnLogWarning;
        public static Action<string> OnLogError;

        public static void Log(object message) 
        {
#if DEBUG_LOG_NORMAL
            OnLog?.Invoke(message?.ToString());
#endif
        }

        public static void LogWarning(object message) 
        {
#if DEBUG_LOG_WARNING
            OnLogWarning?.Invoke(message?.ToString());
#endif
        }

        public static void LogError(object message) 
        {
#if DEBUG_LOG_ERROR
            OnLogError?.Invoke(message?.ToString());
#endif
        }
    }
}
