using System;

namespace Framework.Modules.Network
{
    /// <summary>
    /// 消息协议定义
    /// 包含简单的包头 (Command/Size)
    /// </summary>
    public static class NetworkProtocol
    {
        /// <summary>
        /// 包头大小（4字节 CMD + 4字节 Payload Size）
        /// </summary>
        public const int HeaderSize = 8;

        /// <summary>
        /// 预定义指令集
        /// </summary>
        public static class Cmd
        {
            /// <summary>
            /// 心跳请求
            /// </summary>
            public const int Ping = 0;
            /// <summary>
            /// 心跳响应
            /// </summary>
            public const int Pong = 1;
            /// <summary>
            /// 错误消息
            /// </summary>
            public const int Error = -1;
        }
    }
}
