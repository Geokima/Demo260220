namespace Framework
{
    /// <summary>
    /// 日志接口 (用于跨引擎/跨端)
    /// </summary>
    public interface ILogger : ISystem
    {
        void Log(string message);
        void LogWarning(string message);
        void LogError(string message);
    }
}
