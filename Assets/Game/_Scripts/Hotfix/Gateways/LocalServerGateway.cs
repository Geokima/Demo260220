using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Config;
using Framework.Modules.Network;
using Framework.Modules.Timer;
using Game.Auth;
using Game.Base;
using Game.Consts;
using Game.Config;
using Game.DTOs;
using Game.Gateways;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Gateways
{
    /// <summary>
    /// 本地服务器模拟网关 - 负责路由路由分发与会话管理
    /// </summary>
    public class LocalServerGateway : AbstractSystem, IServerGateway
    {
        #region Fields

        private const float MAIL_TEST_INTERVAL = 30f;
        private LocalDatabase _database;
        private IConfigSystem _configSystem;
        
        // 会话管理
        private int _currentUserId = 1;
        private bool _isLoggedIn;
        private Dictionary<string, int> _tokenToUserId = new();
        
        private Dictionary<string, Action<JToken>> _wsHandlers = new();

        #endregion

        #region Init

        public override void Init()
        {
            _database = new LocalDatabase();
            _configSystem = this.GetSystem<IConfigSystem>();
            _database.Init(_configSystem);
        }

        public override void PostInit()
        {
            this.GetSystem<ITimerSystem>()?.AddTimer(MAIL_TEST_INTERVAL, -1, () =>
            {
                if (_isLoggedIn)
                {
                    var ctx = CreateContext(_currentUserId);
                    MailController.GenerateAndAddTestMail(ctx);
                }
            });
        }

        public override void Deinit() => _database.Save();

        public void Save() => _database.Save();

        #endregion

        #region IServerGateway

        public string WsUrl { get; set; }
        public NetworkStatus WsStatus => NetworkStatus.Connected;

        public async UniTask<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request)
            where TResponse : class
        {
            // 自动注入 Token (模仿真实网关行为)
            if (request is BaseRequest baseReq)
            {
                var accountModel = this.GetModel<AccountModel>();
                if (accountModel.IsLoggedIn && string.IsNullOrEmpty(baseReq.Token))
                {
                    baseReq.Token = accountModel.Token.Value;
                }
            }

            // 1. 获取会话上下文
            int userId = GetUserIdFromToken(request);
            var ctx = CreateContext(userId);

            // 1.5 底层透明化：每次请求前检查重置和过期
            ctx.Db.CheckAllStatusReset(userId);

            // 2. 路由分发 (转发至各业务控制器)
            object result = path switch
            {
                // Auth
                "/login" => ProcessLogin(ctx, request as LoginRequest),
                "/register" => AuthController.HandleRegister(ctx, request as RegisterRequest),
                "/logout" => ProcessLogout(ctx, request as LogoutRequest),
                
                // Player
                "/resource/get" => PlayerController.HandleGetResource(ctx, request as BaseRequest),
                
                // Inventory
                "/inventory/get" => InventoryController.HandleGetInventory(ctx, request as BaseRequest),
                "/inventory/add" => InventoryController.HandleAddItem(ctx, request as AddItemRequest),
                "/inventory/remove" => InventoryController.HandleRemoveItem(ctx, request as RemoveItemRequest),
                "/inventory/use" => InventoryController.HandleUseItem(ctx, request as UseItemRequest),
                
                // Mail
                "/mail/list" => MailController.HandleGetMailList(ctx, request as MailOpRequest),
                "/mail/read" => MailController.HandleReadMail(ctx, request as MailOpRequest),
                "/mail/receive" => MailController.HandleReceiveAttachment(ctx, request as MailOpRequest),
                "/mail/delete" => MailController.HandleDeleteMail(ctx, request as MailOpRequest),
                
                // Mission
                "/mission/list" => MissionController.HandleGetMissions(ctx, request as BaseRequest),
                "/mission/claim" => MissionController.HandleClaimMission(ctx, request as ClaimMissionRequest),
                "/mission/progress" => MissionController.HandleProgress(ctx, request as MissionProgressRequest),

                // Shop
                "/shop/list" => ShopController.HandleGetShopList(ctx, request as ShopListRequest),
                "/shop/buy" => ShopController.HandleBuy(ctx, request as ShopBuyRequest),
                "/shop/refresh" => ShopController.HandleRefresh(ctx, request as ShopRefreshRequest),
                
                _ => throw new Exception($"[LocalServerGateway] 404 Not Found: {path}")
            };

            Debug.Log($"[LocalServerGateway] Route: {path} | User: {userId}");
            await UniTask.Delay(10);
            return result as TResponse;
        }

        public async UniTask<TResponse> PostAsync<TResponse>(string path) where TResponse : class
        {
            return await PostAsync<object, TResponse>(path, new BaseRequest());
        }

        public UniTask<bool> ConnectWsAsync(string url = null) => UniTask.FromResult(true);
        public void DisconnectWs() { }
        public void SendWsMessage(string msgType, object data) { }
        public void RegisterWsHandler(string msgType, Action<JToken> handler) => _wsHandlers[msgType] = handler;
        public void UnregisterWsHandler(string msgType, Action<JToken> handler) => _wsHandlers.Remove(msgType);

        #endregion

        #region Internal Logic

        private int GetUserIdFromToken(object request)
        {
            if (request is BaseRequest baseReq && !string.IsNullOrEmpty(baseReq.Token))
            {
                if (_tokenToUserId.TryGetValue(baseReq.Token, out var userId))
                    return userId;
            }
            return _currentUserId;
        }

        private ServerContext CreateContext(int userId)
        {
            return new ServerContext
            {
                UserId = userId,
                Db = _database,
                Configs = _configSystem,
                BroadcastAction = SimulateBroadcast,
                DirectPushAction = SimulateDirectPush
            };
        }

        /// <summary>
        /// 全体广播
        /// </summary>
        private void SimulateBroadcast(string msgType, object data)
        {
            if (_wsHandlers.TryGetValue(msgType, out var handler))
                handler.Invoke(JToken.FromObject(data));
        }

        /// <summary>
        /// 单体推送
        /// </summary>
        private void SimulateDirectPush(int targetUserId, string msgType, object data)
        {
            if (targetUserId == _currentUserId)
            {
                SimulateBroadcast(msgType, data);
            }
        }

        #endregion

        #region Auth Override (处理 Token 持久化)

        private LoginResponse ProcessLogin(ServerContext ctx, LoginRequest req)
        {
            var resp = AuthController.HandleLogin(ctx, req);
            if (resp.Code == 0 && resp.Data != null)
            {
                _currentUserId = resp.Data.UserId;
                _isLoggedIn = true;
                _tokenToUserId[resp.Data.Token] = resp.Data.UserId;
            }
            return resp;
        }

        private LogoutResponse ProcessLogout(ServerContext ctx, LogoutRequest req)
        {
            _isLoggedIn = false;
            if (req != null && !string.IsNullOrEmpty(req.Token))
                _tokenToUserId.Remove(req.Token);
            
            return AuthController.HandleLogout(ctx, req);
        }

        #endregion
    }
}
