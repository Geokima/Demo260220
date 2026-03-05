using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Framework;
using static Framework.Logger;

namespace Framework.Modules.Network
{
    /// <summary>
    /// 网络系统实现类
    /// </summary>
    public class NetworkSystem : AbstractSystem, INetworkSystem, IUpdateable
    {
        #region Fields & Properties

        // --- 移除了静态 Action 事件，改为使用 Framework 的 SendEvent ---

        private INetworkClient _client;
        private string _url;
        private NetworkStatus _status = NetworkStatus.Disconnected;

        // 配置参数
        private const int MaxReconnectAttempts = 10;
        private const int BaseDelayMs = 1000;
        private const int MaxDelayMs = 10000;
        private const int HeartbeatIntervalMs = 5000;
        private const int HeartbeatTimeoutMs = 15000;

        // 状态管理
        private int _reconnectAttempts;
        private float _lastHeartbeatSentTime;
        private float _lastHeartbeatReceivedTime;
        private readonly Random _random = new Random();
        private readonly Dictionary<int, Action<byte[]>> _handlers = new Dictionary<int, Action<byte[]>>();

        /// <inheritdoc />
        public NetworkStatus Status => _status;

        /// <inheritdoc />
        public string Url { get => _url; set => _url = value; }

        #endregion

        #region Lifecycle

        /// <inheritdoc />
        public override void Init()
        {
            _client = new WebSocketClient();
            _status = NetworkStatus.Disconnected;
        }

        /// <inheritdoc />
        public override void Deinit()
        {
            _client?.Dispose();
            _client = null;
            _handlers.Clear();
        }

        /// <inheritdoc />
        public void OnUpdate()
        {
            if (_client == null) return;

            while (_client.TryGetMessage(out var pkg))
            {
                HandleMessage(pkg);
            }

            if (_status == NetworkStatus.Connected)
            {
                UpdateHeartbeat();
            }
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public async UniTask<bool> Connect(string url = null)
        {
            if (!string.IsNullOrEmpty(url))
            {
                _url = url;
            }

            if (string.IsNullOrEmpty(_url))
            {
                LogError("[Network] Connect failed: URL is null or empty.");
                return false;
            }

            _reconnectAttempts = 0;
            UpdateStatus(NetworkStatus.Connecting);

            try
            {
                await _client.ConnectAsync(_url);
                OnConnectSuccess();
                return true;
            }
            catch (Exception ex)
            {
                LogError($"[Network] Connection failed: {ex.Message}");
                UpdateStatus(NetworkStatus.Disconnected);
                TriggerReconnect();
                return false;
            }
        }

        /// <inheritdoc />
        public void OnApplicationResume()
        {
            if (_status != NetworkStatus.Connected) return;

            if (!_client.IsConnected)
            {
                LogWarning("[Network] Connection lost during background. Reconnecting...");
                TriggerReconnect();
            }
            else
            {
                _lastHeartbeatSentTime = 0; // 立即触发一次心跳探测
            }
        }

        /// <inheritdoc />
        public void RegisterHandler(int cmd, Action<byte[]> handler)
        {
            _handlers[cmd] = handler;
        }

        /// <inheritdoc />
        public void Send(int cmd, string json)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
            Send(cmd, data);
        }

        /// <inheritdoc />
        public void Send(int cmd, byte[] data)
        {
            if (_client == null || !_client.IsConnected)
            {
                LogWarning($"[Network] Send failed: WebSocket not connected. Cmd: {cmd}");
                return;
            }
            _client.SendAsync(cmd, data).Forget();
        }

        #endregion

        #region Private Methods

        private void OnConnectSuccess()
        {
            UpdateStatus(NetworkStatus.Connected);
            _lastHeartbeatReceivedTime = Framework.Time.Now;
        }

        private void HandleMessage(NetworkPackage pkg)
        {
            _lastHeartbeatReceivedTime = Framework.Time.Now;

            if (_handlers.TryGetValue(pkg.Cmd, out var handler))
            {
                handler.Invoke(pkg.Payload);
            }

            this.SendEvent(new NetworkMessageReceivedEvent { Cmd = pkg.Cmd, Payload = pkg.Payload });
        }

        private void UpdateHeartbeat()
        {
            float now = Framework.Time.Now;

            if (now - _lastHeartbeatSentTime > HeartbeatIntervalMs / 1000f)
            {
                _lastHeartbeatSentTime = now;
                Send(NetworkProtocol.Cmd.Ping, Array.Empty<byte>());
            }

            if (now - _lastHeartbeatReceivedTime > HeartbeatTimeoutMs / 1000f)
            {
                LogWarning("[Network] Heartbeat timeout. Starting reconnect...");
                TriggerReconnect();
            }
        }

        private void TriggerReconnect()
        {
            _client.Disconnect();
            StartReconnect();
        }

        private void StartReconnect()
        {
            if (_status == NetworkStatus.Reconnecting || _status == NetworkStatus.ReconnectFailed) return;

            _reconnectAttempts = 0;
            ReconnectInternal().Forget();
        }

        private async UniTaskVoid ReconnectInternal()
        {
            while (_reconnectAttempts < MaxReconnectAttempts)
            {
                _reconnectAttempts++;
                UpdateStatus(NetworkStatus.Reconnecting, _reconnectAttempts);

                int delay = (int)(BaseDelayMs * Math.Pow(2, _reconnectAttempts - 1));
                delay = Math.Min(delay, MaxDelayMs);

                // 引入 Jitter (抖动) +/- 20%
                double jitter = 0.8 + (_random.NextDouble() * 0.4); // 0.8 to 1.2
                delay = (int)(delay * jitter);

                Log($"[Network] Reconnect attempt {_reconnectAttempts} in {delay}ms...");
                await UniTask.Delay(delay);

                try
                {
                    await _client.ConnectAsync(_url);
                    OnConnectSuccess();
                    return;
                }
                catch
                {
                    // 继续重试
                }
            }

            UpdateStatus(NetworkStatus.ReconnectFailed);
            LogError("[Network] Max reconnect attempts reached. Connection failed.");
        }

        private void UpdateStatus(NetworkStatus newStatus, int retryCount = 0)
        {
            _status = newStatus;
            this.SendEvent(new NetworkStatusUpdateEvent { NewStatus = newStatus, RetryCount = retryCount });
            Log($"[Network] Status changed to: {newStatus} (Retry: {retryCount})");
        }

        #endregion
    }
}
