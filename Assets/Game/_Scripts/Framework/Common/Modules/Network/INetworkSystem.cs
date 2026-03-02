using System;
using Cysharp.Threading.Tasks;
using Framework;

namespace Framework.Modules.Network
{
    /// <summary>
    /// 网络系统接口
    /// 定义网络模块的高层业务逻辑 (连接、发送、协议分发)
    /// </summary>
    public interface INetworkSystem : ISystem
    {
        /// <summary>
        /// 当前网络连接状态
        /// </summary>
        NetworkStatus Status { get; }

        /// <summary>
        /// 发起网络连接
        /// </summary>
        /// <param name="url">服务器地址</param>
        /// <returns>连接任务</returns>
        UniTask Connect(string url);

        /// <summary>
        /// 注册特定指令的消息处理器
        /// </summary>
        /// <param name="cmd">指令号</param>
        /// <param name="handler">处理逻辑</param>
        void RegisterHandler(int cmd, Action<byte[]> handler);

        /// <summary>
        /// 发送 JSON 字符串消息
        /// </summary>
        /// <param name="cmd">指令号</param>
        /// <param name="json">JSON 字符串</param>
        void Send(int cmd, string json);

        /// <summary>
        /// 发送原始字节流消息
        /// </summary>
        /// <param name="cmd">指令号</param>
        /// <param name="data">原始数据负载</param>
        void Send(int cmd, byte[] data);

        /// <summary>
        /// 当应用从后台恢复时，检查并修复连接
        /// </summary>
        void OnApplicationResume();
    }
}
