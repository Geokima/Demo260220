using System;
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

        [Header("后台保活")]
        [SerializeField] private bool _enableBackgroundReconnect = true;
        [SerializeField] private int _backgroundTimeout = 300; // 后台超过5分钟重新绑定token

        private string _cachedToken; // 缓存的token用于重连
        private bool _wasConnected; // 进入后台前是否已连接
        private float _backgroundTime; // 进入后台的时间

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
            
            // 注册消息处理器
            _webSocket.RegisterHandler("bind_token", OnBindTokenResponseHandler);
            _webSocket.RegisterHandler("force_logout", OnForceLogoutHandler);

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
            
            // 如果有缓存的token，自动绑定
            if (!string.IsNullOrEmpty(_cachedToken))
            {
                BindToken(_cachedToken);
            }
        }

        /// <summary>
        /// 绑定token响应事件
        /// </summary>
        public event System.Action<bool, string> OnBindTokenResponse;

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

        private void OnBindTokenResponseHandler(string json)
        {
            try
            {
                var response = JsonUtility.FromJson<BindTokenResponse>(json);
                bool success = response.code == 0;
                
                if (success)
                {
                    Debug.Log("[WebSocketManager] Token绑定成功");
                }
                else
                {
                    Debug.LogWarning($"[WebSocketManager] Token绑定失败: {response.msg}");
                    // 绑定失败，清除token
                    _cachedToken = null;
                }
                
                OnBindTokenResponse?.Invoke(success, response.msg);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebSocketManager] 解析bind_token响应失败: {e.Message}");
                OnBindTokenResponse?.Invoke(false, "解析响应失败");
            }
        }

        private void OnForceLogoutHandler(string json)
        {
            Debug.Log("[WebSocketManager] 收到强制登出通知");
            _cachedToken = null;
            Disconnect();
        }

        [System.Serializable]
        private class BindTokenResponse
        {
            public string type;
            public int code;
            public string msg;
            public int userId;
        }

        #endregion

        #region 前后台切换处理

        private void OnApplicationPause(bool pause)
        {
            if (!_enableBackgroundReconnect) return;

            if (pause)
            {
                // 进入后台
                _wasConnected = _webSocket != null && _webSocket.IsConnected;
                _backgroundTime = Time.realtimeSinceStartup;
                Debug.Log($"[WebSocketManager] 进入后台，当前连接状态: {_wasConnected}");
            }
            else
            {
                // 回到前台
                float backgroundDuration = Time.realtimeSinceStartup - _backgroundTime;
                Debug.Log($"[WebSocketManager] 回到前台，后台时长: {backgroundDuration:F1}秒");

                if (_wasConnected)
                {
                    // 检查连接是否还在
                    if (_webSocket == null || !_webSocket.IsConnected)
                    {
                        Debug.Log("[WebSocketManager] 检测到连接断开，开始重连");
                        ReconnectAndBindToken().Forget();
                    }
                    else if (backgroundDuration > _backgroundTimeout)
                    {
                        // 后台时间太长，重新绑定token
                        Debug.Log("[WebSocketManager] 后台时间太长，重新绑定token");
                        BindToken(_cachedToken);
                    }
                }
            }
        }

        /// <summary>
        /// 缓存token用于重连
        /// </summary>
        public void SetCachedToken(string token)
        {
            _cachedToken = token;
        }

        /// <summary>
        /// 绑定token到WebSocket连接
        /// </summary>
        public void BindToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                Debug.LogWarning("[WebSocketManager] Token为空，无法绑定");
                return;
            }

            _cachedToken = token;

            if (!_webSocket.IsConnected)
            {
                Debug.LogWarning("[WebSocketManager] WebSocket未连接，先连接再绑定token");
                return;
            }

            var bindMsg = new { type = "bind_token", token = token };
            Send(bindMsg);
            Debug.Log("[WebSocketManager] 发送token绑定请求");
        }

        /// <summary>
        /// 重连并绑定token
        /// </summary>
        private async UniTaskVoid ReconnectAndBindToken()
        {
            if (string.IsNullOrEmpty(_cachedToken))
            {
                Debug.LogWarning("[WebSocketManager] 没有缓存的token，无法自动重连绑定");
                return;
            }

            // 等待重连完成
            int attempts = 0;
            while (!_webSocket.IsConnected && attempts < _maxReconnectAttempts)
            {
                await UniTask.Delay(1000);
                attempts++;
            }

            if (_webSocket.IsConnected)
            {
                // 重连成功，绑定token
                BindToken(_cachedToken);
            }
            else
            {
                Debug.LogError("[WebSocketManager] 重连失败，请手动重新登录");
            }
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
                _webSocket.UnregisterHandler("bind_token", OnBindTokenResponseHandler);
                _webSocket.UnregisterHandler("force_logout", OnForceLogoutHandler);
            }
            
            if (_instance == this)
                _instance = null;
        }
    }
}
