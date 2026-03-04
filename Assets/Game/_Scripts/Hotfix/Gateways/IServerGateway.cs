using System;
using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Network;
using Newtonsoft.Json.Linq;

namespace Game.Gateways
{
    public interface IServerGateway : ISystem
    {
        #region HTTP

        UniTask<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request) where TResponse : class;
        UniTask<TResponse> PostAsync<TResponse>(string path) where TResponse : class;

        #endregion

        #region WebSocket

        NetworkStatus WsStatus { get; }
        string WsUrl { get; set; }
        UniTask<bool> ConnectWsAsync(string url = null);
        void DisconnectWs();
        void SendWsMessage(string msgType, object data);
        void RegisterWsHandler(string msgType, Action<JToken> handler);
        void UnregisterWsHandler(string msgType, Action<JToken> handler);

        #endregion
    }
}
