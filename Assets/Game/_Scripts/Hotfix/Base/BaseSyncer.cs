using System;
using System.Collections.Generic;
using Framework;
using Game.Gateways;
using Newtonsoft.Json.Linq;

namespace Game.Base
{
    public abstract class BaseSyncer : AbstractSystem
    {
        private readonly List<(string msgType, Action<JToken> handler)> _registeredHandlers = new List<(string, Action<JToken>)>();

        protected IServerGateway ServerGateway => this.GetSystem<IServerGateway>();

        /// <summary>
        /// 注册消息监听，并在 Deinit 时自动清理
        /// </summary>
        protected void RegisterWsHandler(string msgType, Action<JToken> handler)
        {
            ServerGateway.RegisterWsHandler(msgType, handler);
            _registeredHandlers.Add((msgType, handler));
        }

        public override void Deinit()
        {
            // 自动清理所有已注册的监听
            foreach (var (msgType, handler) in _registeredHandlers)
            {
                ServerGateway.UnregisterWsHandler(msgType, handler);
            }
            _registeredHandlers.Clear();


            base.Deinit();
        }
    }
}
