using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Http;
using Framework.Modules.Network;
using Game.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Gateways
{
    public class NetworkServerGateway : AbstractSystem, IServerGateway
    {
        private IHttpSystem _httpSystem;
        private INetworkSystem _networkSystem;
        private readonly Dictionary<string, List<Action<JToken>>> _wsHandlers = new Dictionary<string, List<Action<JToken>>>();

        public override void Init()
        {
            _httpSystem = this.GetSystem<IHttpSystem>();
            _networkSystem = this.GetSystem<INetworkSystem>();
            _networkSystem.RegisterHandler(NetworkProtocol.Cmd.Business, OnRawWsMessageReceived);
        }

        public override void Deinit()
        {
            _wsHandlers.Clear();
        }

        #region HTTP

        public async UniTask<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request) where TResponse : class
        {
            try
            {
                var accountModel = this.GetModel<AccountModel>();

                // 统一 Token 注入逻辑 (不再使用原始的字符串搜索判断)
                if (accountModel.IsLoggedIn && request is BaseRequest baseReq)
                {
                    if (string.IsNullOrEmpty(baseReq.Token))
                    {
                        baseReq.Token = accountModel.Token.Value;
                    }
                }

                string json = JsonConvert.SerializeObject(request);
                string result = await _httpSystem.PostAsync(path, json);
                
                if (string.IsNullOrEmpty(result)) return null;

                var response = JsonConvert.DeserializeObject<TResponse>(result);
                
                // 自动化错误审计
                if (response is ResponseBase baseRes && baseRes.Code != 0)
                {
                    Debug.LogWarning($"[NetworkServerGateway] API Error: {path} | Code: {baseRes.Code} | Message: {baseRes.Message}");
                }

                return response;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkServerGateway] HTTP Post failed: {path}, Error: {ex.Message}");
                return null;
            }
        }

        public async UniTask<TResponse> PostAsync<TResponse>(string path) where TResponse : class
        {
            return await PostAsync<object, TResponse>(path, new { });
        }

        #endregion

        #region WebSocket

        public NetworkStatus WsStatus => _networkSystem.Status;

        public string WsUrl
        {
            get => _networkSystem.Url;
            set => _networkSystem.Url = value;
        }

        public async UniTask<bool> ConnectWsAsync(string url = null)
        {
            return await _networkSystem.Connect(url);
        }

        public void DisconnectWs()
        {
            _networkSystem.Deinit();
            _networkSystem.Init();
        }

        public void SendWsMessage(string msgType, object data)
        {
            var jObj = JObject.FromObject(data);
            jObj["type"] = msgType;
            string json = jObj.ToString(Formatting.None);
            _networkSystem.Send(NetworkProtocol.Cmd.Business, json);
        }

        public void RegisterWsHandler(string msgType, Action<JToken> handler)
        {
            if (!_wsHandlers.ContainsKey(msgType))
            {
                _wsHandlers[msgType] = new List<Action<JToken>>();
            }
            _wsHandlers[msgType].Add(handler);
        }

        public void UnregisterWsHandler(string msgType, Action<JToken> handler)
        {
            if (_wsHandlers.TryGetValue(msgType, out var list))
            {
                list.Remove(handler);
            }
        }

        private void OnRawWsMessageReceived(byte[] data)
        {
            try
            {
                string json = System.Text.Encoding.UTF8.GetString(data);
                var obj = JObject.Parse(json);
                string type = obj["type"]?.ToString();

                if (string.IsNullOrEmpty(type)) return;

                if (_wsHandlers.TryGetValue(type, out var handlers))
                {
                    var handlersCopy = new List<Action<JToken>>(handlers);
                    foreach (var handler in handlersCopy)
                    {
                        try { handler.Invoke(obj); }
                        catch (Exception ex) { Debug.LogError($"[NetworkServerGateway] WS Handler Error: {ex.Message}"); }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkServerGateway] Parse WS message error: {ex.Message}");
            }
        }

        #endregion
    }
}
