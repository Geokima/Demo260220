using Cysharp.Threading.Tasks;
using Framework.Modules.Network;
using Game;
using Game.Models;
using UnityEngine;

namespace Game.Tests
{
    /// <summary>
    /// WebSocket 模块集成测试
    /// 挂载到场景中运行测试
    /// </summary>
    public class WebSocketModuleTest : MonoBehaviour
    {
        private INetworkSystem _networkSystem;

        private void Start()
        {
            _networkSystem = GameArchitecture.Instance.GetSystem<INetworkSystem>();

            // 订阅状态更新

            NetworkSystem.OnStatusUpdate += OnStatusUpdate;
            // 订阅全局消息
            NetworkSystem.OnMessageReceived += OnGlobalMessage;

            // 注册特定的业务处理器

            _networkSystem.RegisterHandler(NetworkProtocol.Cmd.Pong, OnPongReceived);
            _networkSystem.RegisterHandler(100, OnCustomMessage); // 假设 100 是业务消息
        }

        private void OnDestroy()
        {
            NetworkSystem.OnStatusUpdate -= OnStatusUpdate;
            NetworkSystem.OnMessageReceived -= OnGlobalMessage;
        }

        private void OnStatusUpdate(NetworkStatus status, int retryCount)
        {
            Debug.Log($"<color=cyan>[WebSocketTest] 状态更新: {status} (重试: {retryCount})</color>");


            if (status == NetworkStatus.Connected)
            {
                // 连接成功，立刻尝试绑定 Token
                AutoBindToken();
            }


            if (status == NetworkStatus.ReconnectFailed)
            {
                Debug.LogError("[WebSocketTest] 重连彻底失败，请检查服务器！");
            }
        }

        private void AutoBindToken()
        {
            var accountModel = GameArchitecture.Instance.GetModel<AccountModel>();
            if (accountModel == null || !accountModel.IsLoggedIn)
            {
                Debug.LogWarning("[WebSocketTest] 尚未登录，无法自动绑定 Token");
                return;
            }

            Debug.Log($"[WebSocketTest] 正在自动绑定 Token: {accountModel.Token}");
            string json = "{\"type\":\"bind_token\", \"token\":\"" + accountModel.Token + "\"}";
            _networkSystem.Send(100, json); // 发送绑定请求
        }

        private void OnGlobalMessage(int cmd, byte[] data)
        {
            // 这里可以处理所有未被特定 Handler 拦截的消息
            // Debug.Log($"[WebSocketTest] 收到全局消息 Cmd: {cmd}");

            // 尝试解析 JSON 看看是否是 kick 消息
            try
            {
                string json = System.Text.Encoding.UTF8.GetString(data);
                if (json.Contains("\"type\":\"kick\""))
                {
                    Debug.LogError($"<color=red>[WebSocketTest] 你已被强制下线！原因: {json}</color>");
                }
            }
            catch { }
        }

        private void OnPongReceived(byte[] data)
        {
            Debug.Log("<color=green>[WebSocketTest] 收到心跳响应 Pong!</color>");
        }

        private void OnCustomMessage(byte[] data)
        {
            string json = System.Text.Encoding.UTF8.GetString(data);
            Debug.Log($"[WebSocketTest] 收到业务消息: {json}");
        }

        [ContextMenu("连接服务器")]
        public void TestConnect()
        {
            string url = GameManager.Instance.TestWebSocketUrl;
            Debug.Log($"[WebSocketTest] 开始连接: {url}");
            _networkSystem.Connect(url).Forget();
        }

        [ContextMenu("断开连接")]
        public void TestDisconnect()
        {
            Debug.Log("[WebSocketTest] 主动断开连接");
            _networkSystem.Deinit();
            _networkSystem.Init();
        }

        [ContextMenu("发送测试消息 (JSON)")]
        public void TestSendJson()
        {
            if (_networkSystem.Status != NetworkStatus.Connected)
            {
                Debug.LogWarning("[WebSocketTest] 请先连接服务器！");
                return;
            }

            string json = "{\"type\":\"echo\", \"content\":\"Hello Framework WebSocket!\"}";
            Debug.Log($"[WebSocketTest] 发送 JSON: {json}");
            _networkSystem.Send(100, json); // 假设 100 是 echo 协议
        }

        [ContextMenu("模拟网络断开 (重连测试)")]
        public void TestSimulateDisconnect()
        {
            Debug.Log("[WebSocketTest] 模拟异常断开...");
            // 注意：这里只是关闭底层，触发重连逻辑
            // 正常开发中这通常由网络波动引起
        }
    }
}
