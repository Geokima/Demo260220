using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using Framework;
using static Framework.Logger;

namespace Framework.Modules.Network
{
    /// <inheritdoc cref="INetworkClient" />
    public class WebSocketClient : INetworkClient
    {
        #region Fields

        private ClientWebSocket _client;
        private CancellationTokenSource _cts;
        private readonly ConcurrentQueue<NetworkPackage> _receivedQueue = new ConcurrentQueue<NetworkPackage>();

        /// <inheritdoc />
        public bool IsConnected => _client?.State == WebSocketState.Open;

        #endregion

        public WebSocketClient()
        {
        }

        #region Public Methods

        /// <inheritdoc />
        public async UniTask ConnectAsync(string url)
        {
            if (IsConnected) return;

            _client?.Dispose();
            _client = new ClientWebSocket();
            _cts = new CancellationTokenSource();

            try
            {
                Log($"[Network] Connecting to {url}...");
                await _client.ConnectAsync(new Uri(url), _cts.Token);
                Log("[Network] WebSocket connected successfully.");

                // 启动后台接收循环
                ReceiveLoop(_cts.Token).Forget();
            }
            catch (Exception e)
            {
                LogError($"[Network] WebSocket connection failed: {e.Message}");
                throw;
            }
        }

        /// <inheritdoc />
        public void Disconnect()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                Log("[Network] Cancelling active WebSocket operations for cleanup/reconnect...");
                _cts.Cancel();
            }

            if (_client != null && IsConnectedState(_client.State))
            {
                try
                {
                    // 仅在状态允许时执行正常关闭握手
                    _client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None).AsUniTask().Forget();
                }
                catch (Exception e)
                {
                    LogWarning($"[Network] CloseAsync skipped: {e.Message}");
                }
            }
            Log("[Network] WebSocket disconnected.");
        }

        /// <inheritdoc />
        public async UniTask SendAsync(int cmd, byte[] data)
        {
            if (!IsConnected) return;

            // 组装包头: [4字节 CMD] + [4字节 Payload Size]
            byte[] header = new byte[NetworkProtocol.HeaderSize];
            BitConverter.GetBytes(cmd).CopyTo(header, 0);
            BitConverter.GetBytes(data.Length).CopyTo(header, 4);

            byte[] fullPacket = new byte[header.Length + data.Length];
            Buffer.BlockCopy(header, 0, fullPacket, 0, header.Length);
            Buffer.BlockCopy(data, 0, fullPacket, header.Length, data.Length);

            try
            {
                await _client.SendAsync(new ArraySegment<byte>(fullPacket), WebSocketMessageType.Binary, true, _cts.Token);
            }
            catch (Exception e)
            {
                LogError($"[Network] WebSocket send failed. Cmd: {cmd}, Error: {e.Message}");
            }
        }

        /// <inheritdoc />
        public bool TryGetMessage(out NetworkPackage msg)
        {
            return _receivedQueue.TryDequeue(out msg);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Disconnect();
            _client?.Dispose();
            _cts?.Dispose();
        }

        #endregion

        #region Private Methods

        private bool IsConnectedState(WebSocketState state)
        {
            return state == WebSocketState.Open || 
                   state == WebSocketState.CloseReceived || 
                   state == WebSocketState.CloseSent;
        }

        private async UniTaskVoid ReceiveLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && IsConnected)
                {
                    // 1. 接收 8 字节包头
                    var headerResult = await ReceiveExactly(NetworkProtocol.HeaderSize, token);
                    if (headerResult == null) break;

                    int cmd = BitConverter.ToInt32(headerResult, 0);
                    int payloadSize = BitConverter.ToInt32(headerResult, 4);

                    // 2. 接收 Payload
                    var payload = await ReceiveExactly(payloadSize, token);
                    if (payload == null) break;

                    // 3. 投递到队列由主线程消费
                    _receivedQueue.Enqueue(new NetworkPackage(cmd, payload));
                }
            }
            catch (Exception e)
            {
                if (!token.IsCancellationRequested)
                    LogWarning($"[Network] WebSocket receive loop interrupted: {e.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        private async UniTask<byte[]> ReceiveExactly(int length, CancellationToken token)
        {
            if (length <= 0) return Array.Empty<byte>();

            byte[] result = new byte[length];
            int received = 0;

            try
            {
                while (received < length)
                {
                    var response = await _client.ReceiveAsync(new ArraySegment<byte>(result, received, length - received), token);
                    if (response.MessageType == WebSocketMessageType.Close) return null;
                    received += response.Count;
                }
            }
            catch (System.Net.Sockets.SocketException)
            {
                return null; // 物理断开，由循环外层处理
            }
            catch (WebSocketException)
            {
                return null;
            }
            catch (OperationCanceledException)
            {
                Log("[Network] WebSocket receive operation cancelled (logical).");
                return null;
            }

            return result;
        }

        #endregion
    }
}
