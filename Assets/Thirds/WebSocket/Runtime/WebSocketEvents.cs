namespace Thirds.WebSocket
{
    /// <summary>
    /// WebSocket 连接成功事件
    /// </summary>
    public struct WebSocketConnectedEvent
    {
    }

    /// <summary>
    /// WebSocket 断开连接事件
    /// </summary>
    public struct WebSocketDisconnectedEvent
    {
        public string Reason;
    }

    /// <summary>
    /// WebSocket 错误事件
    /// </summary>
    public struct WebSocketErrorEvent
    {
        public string ErrorMessage;
    }

    /// <summary>
    /// WebSocket 收到消息事件
    /// </summary>
    public struct WebSocketMessageEvent
    {
        public string Message;
        public string MessageType;
    }

    /// <summary>
    /// WebSocket 开始重连事件
    /// </summary>
    public struct WebSocketReconnectingEvent
    {
        public int AttemptCount;
        public int MaxAttempts;
    }

    /// <summary>
    /// WebSocket 重连成功事件
    /// </summary>
    public struct WebSocketReconnectedEvent
    {
        public int AttemptCount;
    }
}
