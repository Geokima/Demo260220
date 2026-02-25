using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Thirds.WebSocket
{
    /// <summary>
    /// WebSocket 管理器 - 与框架集成
    /// 自动挂载到场景，提供全局访问
    /// </summary>
    public class WebSocketManager : MonoBehaviour
    {
        private static WebSocketManager _instance;
        public static WebSocketManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("WebSocketManager");
                    _instance = go.AddComponent<WebSocketManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private WebSocketClient _webSocket;
        public WebSocketClient WebSocket => _webSocket;

        [Header("连接配置")]
        [SerializeField] private string _serverUrl = "ws://localhost:8080/ws";
        [SerializeField] private bool _autoConnect = false;
        
        [Header("重连配置")]
        [SerializeField] private bool _autoReconnect = true;
        [SerializeField] private int _reconnectInterval = 3;
        [SerializeField] private int _maxReconnectAttempts = 5;
        
        [Header("心跳配置")]
        [SerializeField] private bool _enableHeartbeat = true;
        [SerializeField] private int _heartbeatInterval = 30;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }

        private void Initialize()
        {
            _webSocket = gameObject.AddComponent<WebSocketClient>();

            // 设置配置
            _webSocket.SetAutoReconnect(_autoReconnect);
            _webSocket.SetReconnectInterval(_reconnectInterval);
            _webSocket.SetMaxReconnectAttempts(_maxReconnectAttempts);
            _webSocket.SetEnableHeartbeat(_enableHeartbeat);
            _webSocket.SetHeartbeatInterval(_heartbeatInterval);

            // 订阅事件
            _webSocket.OnConnected += OnConnected;
            _webSocket.OnDisconnected += OnDisconnected;
            _webSocket.OnError += OnError;
            _webSocket.OnMessageReceived += OnMessageReceived;

            if (_autoConnect)
                Connect(_serverUrl);
        }

        #region 公共方法

        /// <summary>
        /// 连接到服务器
        /// </summary>
        public void Connect(string url)
        {
            _webSocket?.Connect(url);
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            _webSocket?.Disconnect();
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public void Send(string message)
        {
            _webSocket?.Send(message);
        }

        /// <summary>
        /// 发送 JSON 消息
        /// </summary>
        public void Send<T>(T data) where T : class
        {
            _webSocket?.Send(data);
        }

        /// <summary>
        /// 发送字节数组（用于 protobuf）
        /// </summary>
        public void SendBytes(byte[] data)
        {
            _webSocket?.SendBytes(data);
        }

        /// <summary>
        /// 注册消息处理器
        /// </summary>
        public void RegisterHandler(string messageType, System.Action<string> handler)
        {
            _webSocket?.RegisterHandler(messageType, handler);
        }

        /// <summary>
        /// 注销消息处理器
        /// </summary>
        public void UnregisterHandler(string messageType, System.Action<string> handler)
        {
            _webSocket?.UnregisterHandler(messageType, handler);
        }

        #endregion

        #region 事件处理

        private void OnConnected()
        {
            Debug.Log("[WebSocketManager] Connected to server");
            // 可以在这里发送登录验证消息
        }

        private void OnDisconnected()
        {
            Debug.Log("[WebSocketManager] Disconnected from server");
        }

        private void OnError(string error)
        {
            Debug.LogError($"[WebSocketManager] Error: {error}");
        }

        private void OnMessageReceived(string message)
        {
            Debug.Log($"[WebSocketManager] Received: {message}");
        }

        #endregion

        private void OnDestroy()
        {
            if (_webSocket != null)
            {
                _webSocket.OnConnected -= OnConnected;
                _webSocket.OnDisconnected -= OnDisconnected;
                _webSocket.OnError -= OnError;
                _webSocket.OnMessageReceived -= OnMessageReceived;
            }
            
            if (_instance == this)
                _instance = null;
        }
    }
}
