using UnityEngine;
using Thirds.WebSocket;
using Framework.Utils;

namespace Game.Tests
{
    /// <summary>
    /// WebSocket 测试组件
    /// 挂载到场景中的空物体上进行测试
    /// </summary>
    public class WebSocketTest : MonoBehaviour
    {
        [Header("连接配置")]
        [SerializeField] private string _serverUrl = "ws://localhost:8081";

        [Header("测试消息")]
        [SerializeField] private string _testMessage = "Hello WebSocket!";

        private string _status = "未连接";
        private string _lastReceived = "";
        private Vector2 _scrollPosition;

        void Start()
        {
            // 订阅事件
            WebSocketManager.Instance.WebSocket.OnConnected += OnConnected;
            WebSocketManager.Instance.WebSocket.OnDisconnected += OnDisconnected;
            WebSocketManager.Instance.WebSocket.OnError += OnError;
            WebSocketManager.Instance.WebSocket.OnMessageReceived += OnMessageReceived;
            
            // 注册各类消息处理器
            WebSocketManager.Instance.RegisterHandler("login", OnLoginResponse);
            WebSocketManager.Instance.RegisterHandler("heartbeat", OnHeartbeat);
            WebSocketManager.Instance.RegisterHandler("chat", OnChat);
            WebSocketManager.Instance.RegisterHandler("player_sync", OnPlayerSync);
            WebSocketManager.Instance.RegisterHandler("echo", OnEcho);
            WebSocketManager.Instance.RegisterHandler("announcement", OnAnnouncement);
            WebSocketManager.Instance.RegisterHandler("error", OnServerError);
        }

        void OnDestroy()
        {
            // 取消订阅
            WebSocketManager.Instance.WebSocket.OnConnected -= OnConnected;
            WebSocketManager.Instance.WebSocket.OnDisconnected -= OnDisconnected;
            WebSocketManager.Instance.WebSocket.OnError -= OnError;
            WebSocketManager.Instance.WebSocket.OnMessageReceived -= OnMessageReceived;

            WebSocketManager.Instance.Disconnect();
        }
        
        private void OnLoginResponse(string json)
        {
            Debug.Log($"[WebSocketTest] 登录响应: {json}");
            _lastReceived = $"[登录] {json}";
        }
        
        private void OnHeartbeat(string json)
        {
            Debug.Log($"[WebSocketTest] 心跳: {json}");
        }
        
        private void OnChat(string json)
        {
            Debug.Log($"[WebSocketTest] 聊天: {json}");
            _lastReceived = $"[聊天] {json}";
        }
        
        private void OnPlayerSync(string json)
        {
            Debug.Log($"[WebSocketTest] 玩家同步: {json}");
            _lastReceived = $"[同步] {json}";
        }
        
        private void OnEcho(string json)
        {
            Debug.Log($"[WebSocketTest] 回声: {json}");
            _lastReceived = $"[回声] {json}";
        }
        
        private void OnAnnouncement(string json)
        {
            var msg = JsonUtility.FromJson<AnnouncementMessage>(json);
            Debug.Log($"[WebSocketTest] 【系统公告】 {msg.message}");
            _lastReceived = $"[公告] {msg.message}";
        }
        
        private void OnServerError(string json)
        {
            Debug.LogError($"[WebSocketTest] 服务器错误: {json}");
            _lastReceived = $"[错误] {json}";
        }

        #region 按钮操作

        [Button]
        public void Connect()
        {
            Debug.Log($"[WebSocketTest] 正在连接: {_serverUrl}");
            _status = "连接中...";
            WebSocketManager.Instance.Connect(_serverUrl);
        }

        [Button]
        public void Disconnect()
        {
            Debug.Log("[WebSocketTest] 断开连接");
            WebSocketManager.Instance.Disconnect();
        }

        [Button]
        public void SendTestMessage()
        {
            if (!WebSocketManager.Instance.WebSocket.IsConnected)
            {
                Debug.LogWarning("[WebSocketTest] 未连接，无法发送");
                return;
            }

            var msg = new TestMessage
            {
                type = "echo",
                content = _testMessage,
                timestamp = System.DateTime.Now.ToString()
            };

            string json = JsonUtility.ToJson(msg);
            Debug.Log($"[WebSocketTest] 发送: {json}");
            WebSocketManager.Instance.Send(json);
        }

        #endregion

        #region 事件处理

        private void OnConnected()
        {
            _status = "已连接";
            Debug.Log("[WebSocketTest] 连接成功!");
        }

        private void OnDisconnected()
        {
            _status = "已断开";
            Debug.Log("[WebSocketTest] 连接断开");
        }

        private void OnError(string error)
        {
            _status = $"错误: {error}";
            Debug.LogError($"[WebSocketTest] 错误: {error}");
        }

        private void OnMessageReceived(string message)
        {
            _lastReceived = message;
            Debug.Log($"[WebSocketTest] 收到: {message}");
        }

        #endregion

        #region GUI

        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 500), "WebSocket 测试", "box");

            // 状态显示
            GUILayout.Label($"状态: {_status}", GetStatusStyle());
            GUILayout.Space(10);

            // 服务器地址
            GUILayout.Label("服务器地址:");
            _serverUrl = GUILayout.TextField(_serverUrl);
            GUILayout.Space(10);

            // 连接按钮
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("连接", GUILayout.Height(40)))
                Connect();
            if (GUILayout.Button("断开", GUILayout.Height(40)))
                Disconnect();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // 发送消息
            GUILayout.Label("测试消息:");
            _testMessage = GUILayout.TextField(_testMessage);
            if (GUILayout.Button("发送消息", GUILayout.Height(40)))
                SendTestMessage();
            GUILayout.Space(10);

            // 收到的消息
            GUILayout.Label("收到的消息:");
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(150));
            GUILayout.TextArea(_lastReceived, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private GUIStyle GetStatusStyle()
        {
            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = 16;
            style.fontStyle = FontStyle.Bold;

            if (_status.Contains("已连接"))
                style.normal.textColor = Color.green;
            else if (_status.Contains("错误"))
                style.normal.textColor = Color.red;
            else if (_status.Contains("连接中"))
                style.normal.textColor = Color.yellow;
            else
                style.normal.textColor = Color.gray;

            return style;
        }

        #endregion

        [System.Serializable]
        private class TestMessage
        {
            public string type;
            public string content;
            public string timestamp;
        }
        
        [System.Serializable]
        private class AnnouncementMessage
        {
            public string type;
            public string message;
            public string timestamp;
        }
    }
}
