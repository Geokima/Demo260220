using Framework;

namespace Framework.Modules.Network
{
    /// <summary>
    /// 网络状态更新事件
    /// </summary>
    public struct NetworkStatusUpdateEvent : IEvent
    {
        /// <summary>
        /// 新状态
        /// </summary>
        public NetworkStatus NewStatus;
        
        /// <summary>
        /// 当前重试次数
        /// </summary>
        public int RetryCount;
    }

    /// <summary>
    /// 网络消息接收事件
    /// </summary>
    public struct NetworkMessageReceivedEvent : IEvent
    {
        /// <summary>
        /// 指令号
        /// </summary>
        public int Cmd;

        /// <summary>
        /// 原始数据负载
        /// </summary>
        public byte[] Payload;
    }
}
