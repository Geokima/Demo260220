using System;
using Cysharp.Threading.Tasks;

namespace Framework.Modules.Network
{
    /// <summary>
    /// 网络驱动接口
    /// 抽象底层传输协议 (WebSocket, UDP, KCP, TCP 等)
    /// </summary>
    public interface INetworkClient : IDisposable
    {
        /// <summary>
        /// 检查当前是否处于连接开启状态
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 异步连接到指定地址
        /// </summary>
        /// <param name="url">服务器地址</param>
        /// <returns>连接任务</returns>
        UniTask ConnectAsync(string url);

        /// <summary>
        /// 主动断开并清理连接
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 异步发送二进制数据包
        /// </summary>
        /// <param name="cmd">指令号</param>
        /// <param name="data">数据负载</param>
        /// <returns>发送任务</returns>
        UniTask SendAsync(int cmd, byte[] data);

        /// <summary>
        /// 尝试从接收队列中提取一个消息包 (由主线程 Update 消费)
        /// </summary>
        /// <param name="msg">输出的消息包</param>
        /// <returns>是否获取成功</returns>
        bool TryGetMessage(out NetworkPackage msg);
    }
}
