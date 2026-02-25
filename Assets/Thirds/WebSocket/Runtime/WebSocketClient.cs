using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Thirds.WebSocket
{
    /// <summary>
    /// WebSocket 客户端 - 使用 Unity 原生 API
    /// 无需第三方库，支持 WebGL
    /// </summary>
    public class WebSocketClient : MonoBehaviour
    {
        private WebSocketConnection _connection;
        private string _url;
        private Dictionary<string, List<Action<string>>> _messageHandlers = new();
        private Queue<WebSocketMessage> _messageQueue = new();
        private object _lockObj = new object();

        // 配置
        [SerializeField] private bool _autoReconnect = true;
        [SerializeField] private int _reconnectInterval = 3;
        [SerializeField] private int _maxReconnectAttempts = 5;
        [SerializeField] private bool _enableHeartbeat = true;
        [SerializeField] private int _heartbeatInterval = 30;

        // 状态
        public bool IsConnected => _connection?.State == System.Net.WebSockets.WebSocketState.Open;
        public bool IsConnecting => _connection?.State == System.Net.WebSockets.WebSocketState.Connecting;
        public int ReconnectCount { get; private set; }

        // 事件
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        public event Action<string> OnMessageReceived;
        public event Action<byte[]> OnBytesReceived;

        private CancellationTokenSource _heartbeatCts;
        private CancellationTokenSource _reconnectCts;

        #region 配置方法

        public void SetAutoReconnect(bool value) => _autoReconnect = value;
        public void SetReconnectInterval(int seconds) => _reconnectInterval = seconds;
        public void SetMaxReconnectAttempts(int count) => _maxReconnectAttempts = count;
        public void SetEnableHeartbeat(bool value) => _enableHeartbeat = value;
        public void SetHeartbeatInterval(int seconds) => _heartbeatInterval = seconds;

        #endregion

        #region 连接管理

        public void Connect(string url)
        {
            if (IsConnected || IsConnecting)
            {
                Debug.LogWarning("[WebSocket] Already connected or connecting");
                return;
            }

            _url = url;
            ReconnectCount = 0;

            InitializeConnection();
        }

        public void Disconnect()
        {
            _autoReconnect = false;
            _reconnectCts?.Cancel();
            _heartbeatCts?.Cancel();

            _connection?.Close();
            _connection = null;

            Debug.Log("[WebSocket] Disconnected");
        }

        private void InitializeConnection()
        {
            _connection = new WebSocketConnection(_url);

            _connection.OnOpen += () =>
            {
                Debug.Log("[WebSocket] Connected");
                ReconnectCount = 0;
                OnConnected?.Invoke();

                if (_enableHeartbeat)
                    StartHeartbeat().Forget();
            };

            _connection.OnClose += (code, reason) =>
            {
                Debug.Log($"[WebSocket] Closed: {code} - {reason}");
                OnDisconnected?.Invoke();
                _heartbeatCts?.Cancel();

                if (_autoReconnect && ReconnectCount < _maxReconnectAttempts)
                    TryReconnect().Forget();
            };

            _connection.OnError += (error) =>
            {
                Debug.LogError($"[WebSocket] Error: {error}");
                OnError?.Invoke(error);
            };

            _connection.OnMessage += (data) =>
            {
                // 触发字节消息事件（用于 protobuf）
                OnBytesReceived?.Invoke(data);

                // 尝试解析为文本消息
                string message = Encoding.UTF8.GetString(data);
                lock (_lockObj)
                {
                    _messageQueue.Enqueue(new WebSocketMessage
                    {
                        Data = message,
                        Time = DateTime.Now
                    });
                }
            };

            _connection.Connect();
        }

        #endregion

        #region 消息发送

        public void Send(string message)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[WebSocket] Not connected, cannot send message");
                return;
            }

            byte[] data = Encoding.UTF8.GetBytes(message);
            _connection.Send(data);
        }

        public void Send<T>(T data) where T : class
        {
            string json = JsonUtility.ToJson(data);
            Send(json);
        }

        /// <summary>
        /// 发送字节数组（用于 protobuf）
        /// </summary>
        public void SendBytes(byte[] data)
        {
            if (!IsConnected)
            {
                Debug.LogWarning("[WebSocket] Not connected, cannot send bytes");
                return;
            }

            _connection.Send(data);
        }

        #endregion

        #region 消息接收

        private void Update()
        {
            ProcessMessageQueue();
        }

        private void ProcessMessageQueue()
        {
            lock (_lockObj)
            {
                while (_messageQueue.Count > 0)
                {
                    var msg = _messageQueue.Dequeue();
                    HandleMessage(msg.Data);
                }
            }
        }

        private void HandleMessage(string data)
        {
            OnMessageReceived?.Invoke(data);

            try
            {
                var baseMsg = JsonUtility.FromJson<WebSocketBaseMessage>(data);
                if (!string.IsNullOrEmpty(baseMsg.type) && _messageHandlers.ContainsKey(baseMsg.type))
                {
                    var handlers = _messageHandlers[baseMsg.type];
                    foreach (var handler in handlers)
                    {
                        handler?.Invoke(data);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WebSocket] Failed to parse message: {e.Message}");
            }
        }

        public void RegisterHandler(string messageType, Action<string> handler)
        {
            if (!_messageHandlers.ContainsKey(messageType))
                _messageHandlers[messageType] = new List<Action<string>>();

            _messageHandlers[messageType].Add(handler);
        }

        public void UnregisterHandler(string messageType, Action<string> handler)
        {
            if (_messageHandlers.ContainsKey(messageType))
                _messageHandlers[messageType].Remove(handler);
        }

        #endregion

        #region 心跳与重连

        private async UniTaskVoid StartHeartbeat()
        {
            _heartbeatCts = new CancellationTokenSource();

            while (!_heartbeatCts.Token.IsCancellationRequested && IsConnected)
            {
                await UniTask.Delay(_heartbeatInterval * 1000, cancellationToken: _heartbeatCts.Token);

                if (IsConnected)
                {
                    Send(new WebSocketBaseMessage { type = "ping" });
                }
            }
        }

        private async UniTaskVoid TryReconnect()
        {
            _reconnectCts = new CancellationTokenSource();
            ReconnectCount++;

            Debug.Log($"[WebSocket] Reconnecting... ({ReconnectCount}/{_maxReconnectAttempts})");

            await UniTask.Delay(_reconnectInterval * 1000, cancellationToken: _reconnectCts.Token);

            if (_autoReconnect && !IsConnected)
            {
                InitializeConnection();
            }
        }

        #endregion

        private void OnDestroy()
        {
            Disconnect();
        }
    }

    /// <summary>
    /// WebSocket 连接封装 - 使用 Unity 原生 ClientWebSocket
    /// </summary>
    public class WebSocketConnection
    {
        private System.Net.WebSockets.ClientWebSocket _webSocket;
        private string _url;
        private CancellationTokenSource _cts;

        public System.Net.WebSockets.WebSocketState State => _webSocket?.State ?? System.Net.WebSockets.WebSocketState.Closed;

        public event Action OnOpen;
        public event Action<ushort, string> OnClose;
        public event Action<string> OnError;
        public event Action<byte[]> OnMessage;

        public WebSocketConnection(string url)
        {
            _url = url;
            _webSocket = new System.Net.WebSockets.ClientWebSocket();
            _cts = new CancellationTokenSource();
        }

        public async void Connect()
        {
            try
            {
                Uri uri = new Uri(_url);
                await _webSocket.ConnectAsync(uri, _cts.Token);
                OnOpen?.Invoke();
                ReceiveLoop().Forget();
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
            }
        }

        public async void Close()
        {
            try
            {
                _cts?.Cancel();
                if (_webSocket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                        "Closing",
                        CancellationToken.None);
                }
                _webSocket?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WebSocket] Close error: {e.Message}");
            }
        }

        public async void Send(byte[] data)
        {
            try
            {
                if (_webSocket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    var segment = new ArraySegment<byte>(data);
                    await _webSocket.SendAsync(
                        segment,
                        System.Net.WebSockets.WebSocketMessageType.Text,
                        true,
                        _cts.Token);
                }
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.Message);
            }
        }

        private async UniTaskVoid ReceiveLoop()
        {
            var buffer = new byte[4096];

            while (_webSocket.State == System.Net.WebSockets.WebSocketState.Open && !_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var segment = new ArraySegment<byte>(buffer);
                    var result = await _webSocket.ReceiveAsync(segment, _cts.Token);

                    if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                    {
                        OnClose?.Invoke((ushort)result.CloseStatus, result.CloseStatusDescription);
                        break;
                    }

                    byte[] receivedData = new byte[result.Count];
                    Array.Copy(buffer, receivedData, result.Count);
                    OnMessage?.Invoke(receivedData);
                }
                catch (Exception e)
                {
                    if (!_cts.Token.IsCancellationRequested)
                        OnError?.Invoke(e.Message);
                    break;
                }
            }
        }
    }

    [Serializable]
    public class WebSocketBaseMessage
    {
        public string type;
    }

    public class WebSocketMessage
    {
        public string Data;
        public DateTime Time;
    }
}
