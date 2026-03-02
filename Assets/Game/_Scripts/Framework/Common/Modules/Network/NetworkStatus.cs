namespace Framework.Modules.Network
{
    /// <summary>
    /// 网络连接状态枚举
    /// </summary>
    public enum NetworkStatus
    {
        /// <summary>
        /// 未连接
        /// </summary>
        Disconnected,

        /// <summary>
        /// 正在连接中
        /// </summary>
        Connecting,

        /// <summary>
        /// 已连接
        /// </summary>
        Connected,

        /// <summary>
        /// 正在尝试重连
        /// </summary>
        Reconnecting,

        /// <summary>
        /// 重连失败
        /// </summary>
        ReconnectFailed
    }
}
