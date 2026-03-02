namespace Framework
{
    /// <summary>
    /// 时间提供者接口 (用于跨引擎/跨端)
    /// </summary>
    public interface ITimeProvider : ISystem
    {
        float Time { get; }
        float DeltaTime { get; }
        float RealtimeSinceStartup { get; }
    }
}
