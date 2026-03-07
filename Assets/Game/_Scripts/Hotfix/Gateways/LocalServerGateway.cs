using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Config;
using Framework.Modules.Network;
using Framework.Modules.Timer;
using Game.Auth;
using Game.Consts;
using Game.Config;
using Game.DTOs;
using Game.Procedures;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Gateways
{
    public class LocalServerGateway : AbstractSystem, IServerGateway
    {
        #region Fields

        private const float MAIL_TEST_INTERVAL = 10f;
        private LocalDatabase _database;
        private IConfigSystem _configSystem;
        private int _currentUserId = 1;
        private bool _isLoggedIn;
        private readonly string[] _testTitles = { "欢迎礼包", "每日签到", "活动奖励", "系统通知", "新手礼包", "升级奖励", "节日活动", "神秘奖励" };
        private readonly string[] _testSenders = { "系统", "管理员", "活动中心", "新手导师", "礼包发放" };
        private readonly int[] _testItemIds = { 1001, 1002, 1003, 1004 };
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
                    GenerateAndAddTestMail();
            });
        }

        public override void Deinit()
        {
            _database.Save();
        }

        public void Save() => _database.Save();

        #endregion

        #region IServerGateway

        public string WsUrl { get; set; }
        public NetworkStatus WsStatus => NetworkStatus.Connected;

        public async UniTask<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request) where TResponse : class
        {
            object result = path switch
            {
                "/login" => HandleLogin(request as LoginRequest),
                "/register" => HandleRegister(request as RegisterRequest),
                "/logout" => HandleLogout(request as LogoutRequest),
                "/inventory/get" => HandleGetInventory(),
                "/inventory/add" => HandleAddItem(request as AddItemRequest),
                "/inventory/remove" => HandleRemoveItem(request as RemoveItemRequest),
                "/inventory/use" => HandleUseItem(request as UseItemRequest),
                "/resource/get" => HandleGetResource(),
                "/mail/list" => HandleGetMailList(),
                "/mail/read" => HandleReadMail(request as ReadMailRequest),
                "/mail/receive" => HandleReceiveAttachment(request as ReceiveAttachmentRequest),
                "/mail/delete" => HandleDeleteMail(request as DeleteMailRequest),
                "/mail/broadcast" => HandleBroadcastMail(request as BroadcastMailRequest),
                "/shop/list" => HandleGetShopList(request as ShopListRequest),
                "/shop/buy" => HandleBuyShopItem(request as ShopBuyRequest),
                "/shop/refresh" => HandleRefreshShop(request as ShopRefreshRequest),
                _ => throw new Exception($"[LocalServerGateway] 未实现路由: {path}")
            };
            Debug.Log($"[LocalServerGateway] 处理路由: {path}");
            await UniTask.Delay(10);
            return result as TResponse;
        }

        public async UniTask<TResponse> PostAsync<TResponse>(string path) where TResponse : class
        {
            return await PostAsync<object, TResponse>(path, new { });
        }

        public UniTask<bool> ConnectWsAsync(string url = null) => UniTask.FromResult(true);
        public void DisconnectWs() { }
        public void SendWsMessage(string msgType, object data) { }

        public void RegisterWsHandler(string msgType, Action<JToken> handler) => _wsHandlers[msgType] = handler;
        public void UnregisterWsHandler(string msgType, Action<JToken> handler) => _wsHandlers.Remove(msgType);

        public void SimulatePush(string msgType, object data)
        {
            if (_wsHandlers.TryGetValue(msgType, out var handler))
                handler.Invoke(JToken.FromObject(data));
        }

        #endregion

        #region Auth

        private LoginResponse HandleLogin(LoginRequest req)
        {
            if (req == null)
                return new LoginResponse { Code = (int)ErrorCode.InvalidParams, Msg = "请求无效" };

            int userId = 0;
            string username = null;

            if (req.UserId > 0)
            {
                userId = req.UserId;
                username = _database.GetPlayer(req.UserId)?.Username;
            }
            else if (!string.IsNullOrEmpty(req.Username))
            {
                username = req.Username;
                userId = _database.GetUserIdByUsername(username);
            }

            if (userId <= 0 || !_database.HasUser(userId))
                return new LoginResponse { Code = (int)ErrorCode.UserNotFound, Msg = "用户不存在" };

            _currentUserId = userId;
            _isLoggedIn = true;
            var token = $"token_{userId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

            Debug.Log($"[LocalServerGateway] 登录: UserId={userId}, Username={username}");

            return new LoginResponse
            {
                Code = 0,
                Msg = "Success",
                Data = new LoginData
                {
                    Token = token,
                    UserId = userId,
                    Username = username,
                    WsUrl = ""
                }
            };
        }

        private RegisterResponse HandleRegister(RegisterRequest req)
        {
            var username = req?.Username ?? $"Player{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var newUserId = _database.GetNextUserId(username);
            Debug.Log($"[LocalServerGateway] 注册新账号: UserId={newUserId}, Username={username}");
            return new RegisterResponse
            {
                Code = 0,
                Msg = "Success",
                Data = new RegisterData { UserId = newUserId }
            };
        }

        private LogoutResponse HandleLogout(LogoutRequest req)
        {
            _isLoggedIn = false;
            return new LogoutResponse { Code = 0, Msg = "Success" };
        }

        #endregion

        #region Player

        private PlayerResponse HandleGetResource() => new PlayerResponse
        {
            Code = 0,
            Data = _database.GetPlayer(_currentUserId)
        };

        #endregion

        #region Inventory

        private GetInventoryResponse HandleGetInventory() => new GetInventoryResponse
        {
            Code = 0,
            Data = _database.GetInventory(_currentUserId)
        };

        private InventoryResponse HandleAddItem(AddItemRequest req)
        {
            if (req == null)
                return new InventoryResponse { Code = (int)ErrorCode.InvalidParams, Msg = "请求无效" };

            _database.AddItem(_currentUserId, req.ItemId, req.Amount, out int remaining, out var changedItems);
            _database.IncrementInventoryRevision(_currentUserId);

            var syncData = new InventorySyncData
            {
                ChangedItems = changedItems,
                Reason = InventorySyncReason.DROP,
                Revision = _database.GetInventory(_currentUserId).Revision
            };
            SimulatePush(NetworkMsgType.InventoryUpdate, syncData);

            Debug.Log($"[LocalServerGateway] 添加物品: itemId={req.ItemId}, amount={req.Amount}");

            return new InventoryResponse
            {
                Code = remaining > 0 ? 2 : 0,
                Msg = remaining > 0 ? "背包已满" : "Success",
                Data = syncData
            };
        }

        private InventoryResponse HandleRemoveItem(RemoveItemRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.Uid))
                return new InventoryResponse { Code = (int)ErrorCode.InvalidParams, Msg = "请求无效" };

            _database.RemoveItem(_currentUserId, req.Uid, req.Amount, out var updatedItem, out var removedUid);
            _database.IncrementInventoryRevision(_currentUserId);

            var syncData = new InventorySyncData
            {
                ChangedItems = updatedItem != null ? new List<ItemData> { updatedItem } : null,
                RemovedUids = !string.IsNullOrEmpty(removedUid) ? new List<string> { removedUid } : null,
                Reason = InventorySyncReason.USE,
                Revision = _database.GetInventory(_currentUserId).Revision
            };
            SimulatePush(NetworkMsgType.InventoryUpdate, syncData);

            return new InventoryResponse
            {
                Code = 0,
                Msg = "Success",
                Data = syncData
            };
        }

        private UseItemResponse HandleUseItem(UseItemRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.Uid))
                return new UseItemResponse { Code = (int)ErrorCode.InvalidParams, Msg = "请求无效" };

            _database.UseItem(_currentUserId, req.Uid, req.Amount, out var updatedItem, out var effects);
            _database.IncrementInventoryRevision(_currentUserId);

            var syncData = new InventorySyncData
            {
                ChangedItems = updatedItem != null ? new List<ItemData> { updatedItem } : null,
                RemovedUids = updatedItem == null ? new List<string> { req.Uid } : null,
                Reason = InventorySyncReason.USE,
                Revision = _database.GetInventory(_currentUserId).Revision
            };
            SimulatePush(NetworkMsgType.InventoryUpdate, syncData);

            return new UseItemResponse
            {
                Code = 0,
                Msg = "Success",
                Data = effects
            };
        }

        #endregion

        #region Mail

        private void GenerateAndAddTestMail()
        {
            var rand = new System.Random();
            var title = _testTitles[rand.Next(_testTitles.Length)];
            var sender = _testSenders[rand.Next(_testSenders.Length)];
            var itemId = _testItemIds[rand.Next(_testItemIds.Length)];
            var itemCount = rand.Next(1, 6);
            var itemId2 = _testItemIds[rand.Next(_testItemIds.Length)];
            var itemCount2 = rand.Next(1, 6);

            var mail = new MailData
            {
                MailId = $"mail_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{rand.Next(1000, 9999)}",
                Title = title,
                Content = $"这是{title}，请查收！",
                Sender = sender,
                CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                IsRead = false,
                IsReceived = false,
                Attachments = new List<MailAttachment>
                {
                    new MailAttachment { ItemId = itemId, Count = itemCount },
                    new MailAttachment { ItemId = itemId2, Count = itemCount2 }
                }
            };

            _database.AddMail(_currentUserId, mail);
            SimulatePush(NetworkMsgType.MailUpdate, new { mailList = _database.GetMails(_currentUserId) });
            Debug.Log($"[LocalServerGateway] 发送测试邮件: {title} x{itemCount}");
        }

        private MailListResponse HandleGetMailList() => new MailListResponse
        {
            Code = 0,
            Data = _database.GetMails(_currentUserId)
        };

        private MailDetailResponse HandleReadMail(ReadMailRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.MailId))
                return new MailDetailResponse { Code = (int)ErrorCode.InvalidParams, Msg = "邮件ID无效" };

            var mail = _database.GetMail(_currentUserId, req.MailId);
            if (mail == null)
                return new MailDetailResponse { Code = (int)ErrorCode.MailNotFound, Msg = "邮件不存在" };

            _database.MarkMailRead(_currentUserId, req.MailId);

            return new MailDetailResponse
            {
                Code = 0,
                Data = mail
            };
        }

        private ReceiveMailAttachmentResponse HandleReceiveAttachment(ReceiveAttachmentRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.MailId))
                return new ReceiveMailAttachmentResponse { Code = (int)ErrorCode.InvalidParams, Msg = "邮件ID无效" };

            var mail = _database.GetMail(_currentUserId, req.MailId);
            if (mail == null)
                return new ReceiveMailAttachmentResponse { Code = (int)ErrorCode.MailNotFound, Msg = "邮件不存在" };

            if (mail.IsReceived)
                return new ReceiveMailAttachmentResponse { Code = (int)ErrorCode.MailAttachmentReceived, Msg = "附件已领取" };

            if (mail.Attachments == null || mail.Attachments.Count == 0)
                return new ReceiveMailAttachmentResponse { Code = (int)ErrorCode.MailNoAttachment, Msg = "无附件可领取" };

            if (!_database.CanReceiveAttachment(_currentUserId, req.MailId))
                return new ReceiveMailAttachmentResponse { Code = (int)ErrorCode.InventoryFull, Msg = "背包空间不足" };

            _database.ReceiveMailAttachment(_currentUserId, req.MailId, out var changedItems);
            _database.IncrementInventoryRevision(_currentUserId);
 
            // 模拟掉落同步
            SimulatePush(NetworkMsgType.InventoryUpdate, new InventorySyncData 
            { 
                ChangedItems = changedItems,
                Reason = InventorySyncReason.MAIL,
                Revision = _database.GetInventory(_currentUserId).Revision
            });

            return new ReceiveMailAttachmentResponse
            {
                Code = 0,
                Data = _database.GetMails(_currentUserId)
            };
        }

        private MailListResponse HandleDeleteMail(DeleteMailRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.MailId))
                return new MailListResponse { Code = (int)ErrorCode.InvalidParams, Msg = "邮件ID无效" };

            var mail = _database.GetMail(_currentUserId, req.MailId);
            if (mail == null)
                return new MailListResponse { Code = (int)ErrorCode.MailNotFound, Msg = "邮件不存在" };

            _database.DeleteMail(_currentUserId, req.MailId);

            return new MailListResponse
            {
                Code = 0,
                Data = _database.GetMails(_currentUserId)
            };
        }

        #endregion

        #region Broadcast

        private BroadcastMailResponse HandleBroadcastMail(BroadcastMailRequest req)
        {
            if (req == null)
                return new BroadcastMailResponse { Code = (int)ErrorCode.InvalidParams, Msg = "请求无效" };

            _database.BroadcastMail(new BroadcastMailData
            {
                MailId = $"broadcast_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                Title = req.Title,
                Content = req.Content,
                Sender = req.Sender ?? "系统",
                CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Attachments = req.Attachments
            });

            SimulatePush(NetworkMsgType.MailUpdate, new { mailList = _database.GetMails(_currentUserId) });

            Debug.Log($"[LocalServerGateway] 全服广播邮件: {req.Title}");
            return new BroadcastMailResponse { Code = 0, Msg = "Success" };
        }

        #endregion

        #region Shop

        private ShopListResponse HandleGetShopList(ShopListRequest req)
        {
            var shopType = req?.ShopType ?? ShopType.Fixed;
            var data = _database.GetShopItems(_currentUserId, shopType);
            return new ShopListResponse { Code = 0, Data = data };
        }

        private ShopRefreshResponse HandleRefreshShop(ShopRefreshRequest req)
        {
            var shopType = req?.ShopType ?? ShopType.Random;
            var data = _database.RefreshShop(_currentUserId, shopType);

            if (data == null)
                return new ShopRefreshResponse { Code = (int)ErrorCode.ShopRefreshLimitReached, Msg = "刷新次数已达上限" };

            return new ShopRefreshResponse { Code = 0, Data = data };
        }

        private ShopBuyResponse HandleBuyShopItem(ShopBuyRequest req)
        {
            if (req == null || req.ShopItemId <= 0)
                return new ShopBuyResponse { Code = (int)ErrorCode.InvalidParams, Msg = "请求无效" };

            if (req.Count <= 0)
                req.Count = 1;

            var config = _configSystem.Get<ShopItemConfig>(req.ShopItemId);
            if (config == null)
                return new ShopBuyResponse { Code = (int)ErrorCode.ShopItemNotFound, Msg = "商品不存在" };

            var shopType = config.ShopType;
            var success = _database.PurchaseItem(_currentUserId, req.ShopItemId, req.Count, config,
                out var updatedPlayer, out var updatedInventory, out var errorMsg);

            if (!success)
            {
                int errorCode = errorMsg switch
                {
                    "金币不足" => (int)ErrorCode.InsufficientGold,
                    "钻石不足" => (int)ErrorCode.InsufficientDiamond,
                    "已达到限购上限" => (int)ErrorCode.ShopLimitReached,
                    _ => (int)ErrorCode.Failed
                };
                return new ShopBuyResponse { Code = errorCode, Msg = errorMsg };
            }

            SimulatePush(NetworkMsgType.PlayerSync, updatedPlayer);
            SimulatePush(NetworkMsgType.InventoryUpdate, new InventorySyncData
            {
                ChangedItems = updatedInventory.Items?.ToList(),
                Reason = InventorySyncReason.BUY
            });

            return new ShopBuyResponse
            {
                Code = 0,
                Data = new ShopBuyResponseData
                {
                    PlayerSync = updatedPlayer,
                    InventorySync = new InventorySyncData
                    {
                        ChangedItems = updatedInventory.Items?.ToList(),
                        Reason = InventorySyncReason.BUY
                    },
                    ShopSync = _database.GetShopItems(_currentUserId, shopType)
                }
            };
        }

        #endregion
    }
}
