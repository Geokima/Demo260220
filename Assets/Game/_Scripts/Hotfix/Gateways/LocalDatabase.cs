using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Framework.Modules.Config;
using Game.Config;
using Game.Consts;
using Game.DTOs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Gateways
{
    public class LocalDatabase
    {
        #region Fields

        private readonly string _savePath;
        private IConfigSystem _configSystem;
        private Dictionary<int, ItemConfig> _itemConfigs;

        private Dictionary<int, PlayerData> _players = new();
        private Dictionary<string, int> _usernameToUserId = new();
        private Dictionary<int, InventoryData> _inventories = new();
        private Dictionary<int, MailListData> _mails = new();
        private List<BroadcastMailData> _broadcastMails = new();
        private Dictionary<int, List<string>> _readBroadcastMailIds = new();
        private Dictionary<int, Dictionary<int, int>> _purchaseHistory = new();
        private long _lastDailyResetTime = 0;
        private int _shopRefreshCount = 0;
        private int _nextUserId = 1;

        private const int MAIL_MAX_COUNT = 10;

        #endregion

        #region Init

        public LocalDatabase()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "local_save.json");
        }

        public void Init(IConfigSystem configSystem)
        {
            _configSystem = configSystem;
            Load();
        }

        public void Load()
        {
            Debug.Log($"[LocalDatabase] 从存档路径加载: {_savePath}");
            try
            {
                if (File.Exists(_savePath))
                {
                    var json = File.ReadAllText(_savePath);
                    var jo = JObject.Parse(json);

                    _players = new Dictionary<int, PlayerData>();
                    if (jo["Players"] is JObject playersObj)
                    {
                        foreach (var prop in playersObj.Properties())
                        {
                            var key = int.Parse(prop.Name);
                            _players[key] = prop.Value.ToObject<PlayerData>();
                        }
                        foreach (var p in _players)
                        {
                            if (!string.IsNullOrEmpty(p.Value.Username))
                                _usernameToUserId[p.Value.Username.ToLower()] = p.Key;
                        }
                    }

                    _inventories = new Dictionary<int, InventoryData>();
                    if (jo["Inventories"] is JObject invObj)
                    {
                        foreach (var prop in invObj.Properties())
                        {
                            var key = int.Parse(prop.Name);
                            _inventories[key] = prop.Value.ToObject<InventoryData>();
                        }
                    }

                    _mails = new Dictionary<int, MailListData>();
                    if (jo["Mails"] is JObject mailsObj)
                    {
                        foreach (var prop in mailsObj.Properties())
                        {
                            var key = int.Parse(prop.Name);
                            _mails[key] = prop.Value.ToObject<MailListData>();
                        }
                    }

                    _broadcastMails = jo["BroadcastMails"]?.ToObject<List<BroadcastMailData>>() ?? new List<BroadcastMailData>();

                    _readBroadcastMailIds = new Dictionary<int, List<string>>();
                    if (jo["ReadBroadcastMailIds"] is JObject readObj)
                    {
                        foreach (var prop in readObj.Properties())
                        {
                            var key = int.Parse(prop.Name);
                            _readBroadcastMailIds[key] = prop.Value.ToObject<List<string>>();
                        }
                    }

                    _nextUserId = _players.Keys.DefaultIfEmpty(0).Max() + 1;

                    _purchaseHistory = new Dictionary<int, Dictionary<int, int>>();
                    if (jo["PurchaseHistory"] is JObject phObj)
                    {
                        foreach (var userProp in phObj.Properties())
                        {
                            var userId = int.Parse(userProp.Name);
                            var purchases = new Dictionary<int, int>();
                            if (userProp.Value is JObject itemObj)
                            {
                                foreach (var itemProp in itemObj.Properties())
                                {
                                    purchases[int.Parse(itemProp.Name)] = itemProp.Value.ToObject<int>();
                                }
                            }
                            _purchaseHistory[userId] = purchases;
                        }
                    }

                    _lastDailyResetTime = jo["LastDailyResetTime"]?.ToObject<long>() ?? 0;
                    _shopRefreshCount = jo["ShopRefreshCount"]?.ToObject<int>() ?? 0;

                    Debug.Log($"[LocalDatabase] 加载完成，玩家数: {_players.Count}");
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[LocalDatabase] 加载存档失败: {ex.Message}");
            }
        }

        public void Save()
        {
            Debug.Log($"[LocalDatabase] 保存到存档路径: {_savePath}");
            try
            {
                var data = new LocalDatabaseData
                {
                    Players = _players,
                    Inventories = _inventories,
                    Mails = _mails,
                    BroadcastMails = _broadcastMails,
                    ReadBroadcastMailIds = _readBroadcastMailIds,
                    PurchaseHistory = _purchaseHistory,
                    LastDailyResetTime = _lastDailyResetTime,
                    ShopRefreshCount = _shopRefreshCount
                };
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(_savePath, json);
            }
            catch (Exception ex)
            {
                Debug.Log($"[LocalDatabase] 保存存档失败: {ex.Message}");
            }
        }

        #endregion

        #region Player

        public PlayerData GetPlayer(int userId)
        {
            if (!_players.TryGetValue(userId, out var player))
            {
                player = new PlayerData();
                _players[userId] = player;
            }
            return ClonePlayer(player);
        }

        public void UpdatePlayer(int userId, PlayerData data)
        {
            _players[userId] = data;
        }

        public bool HasUser(int userId)
        {
            return _players.ContainsKey(userId);
        }

        public bool HasUserByUsername(string username)
        {
            if (string.IsNullOrEmpty(username)) return false;
            return _usernameToUserId.ContainsKey(username.ToLower());
        }

        public int GetUserIdByUsername(string username)
        {
            if (string.IsNullOrEmpty(username)) return 0;
            return _usernameToUserId.TryGetValue(username.ToLower(), out var id) ? id : 0;
        }

        public int GetNextUserId(string username = null)
        {
            while (_players.ContainsKey(_nextUserId))
                _nextUserId++;

            var player = new PlayerData { Username = username ?? $"Player{_nextUserId}" };
            _players[_nextUserId] = player;

            if (!string.IsNullOrEmpty(username))
                _usernameToUserId[username.ToLower()] = _nextUserId;

            _inventories[_nextUserId] = new InventoryData { Items = Array.Empty<ItemData>(), MaxSlots = 50 };
            _mails[_nextUserId] = new MailListData { Mails = new List<MailData>(), UnreadCount = 0 };
            return _nextUserId;
        }

        #endregion

        #region Inventory

        private InventoryData GetOrCreateInventory(int userId)
        {
            if (!_inventories.TryGetValue(userId, out var inventory))
            {
                inventory = new InventoryData { Items = Array.Empty<ItemData>(), MaxSlots = 50 };
                _inventories[userId] = inventory;
            }
            return inventory;
        }

        public InventoryData GetInventory(int userId)
        {
            return CloneInventory(GetOrCreateInventory(userId));
        }

        public void AddItem(int userId, int itemId, int amount, out int remaining, out List<ItemData> changedItems)
        {
            remaining = amount;
            changedItems = new List<ItemData>();
            var inventory = GetOrCreateInventory(userId);
            var items = inventory.Items?.ToList() ?? new List<ItemData>();
            var maxStack = GetMaxStack(itemId);

            for (int i = 0; i < items.Count && remaining > 0; i++)
            {
                var item = items[i];
                if (item.ItemId == itemId && item.Count < maxStack)
                {
                    int canAdd = maxStack - item.Count;
                    int add = Math.Min(canAdd, remaining);
                    item.Count += add;
                    remaining -= add;
                    
                    if (!changedItems.Contains(item))
                        changedItems.Add(item);
                }
            }

            while (remaining > 0)
            {
                if (items.Count >= inventory.MaxSlots)
                    break;

                var newItem = new ItemData
                {
                    Uid = $"{itemId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{UnityEngine.Random.Range(1000, 9999)}",
                    ItemId = itemId,
                    Count = Math.Min(remaining, maxStack)
                };
                items.Add(newItem);
                changedItems.Add(newItem);
                remaining -= newItem.Count;
            }

            inventory.Items = items.ToArray();
        }

        public void RemoveItem(int userId, string uid, int amount, out ItemData updatedItem, out string removedUid)
        {
            updatedItem = null;
            removedUid = null;

            var inventory = GetOrCreateInventory(userId);
            var items = inventory.Items?.ToList();
            if (items == null) return;

            var target = items.FirstOrDefault(x => x.Uid == uid);
            if (target == null || target.Count < amount) return;

            target.Count -= amount;
            if (target.Count <= 0)
            {
                removedUid = uid;
                items.Remove(target);
            }
            else
            {
                updatedItem = target;
            }

            inventory.Items = items.ToArray();
        }

        public void UseItem(int userId, string uid, int amount, out ItemData updatedItem, out List<ItemEffect> effects)
        {
            updatedItem = null;
            effects = new List<ItemEffect>();

            var inventory = GetOrCreateInventory(userId);
            var items = inventory.Items?.ToList();
            if (items == null) return;

            var target = items.FirstOrDefault(x => x.Uid == uid);
            if (target == null || target.Count < amount) return;

            target.Count -= amount;
            if (target.Count <= 0)
                items.Remove(target);

            inventory.Items = items.ToArray();
            updatedItem = target.Count > 0 ? target : null;

            var itemConfig = GetItemConfig(target.ItemId);
            if (itemConfig != null && itemConfig.EffectId > 0)
            {
                effects.Add(new ItemEffect
                {
                    EffectId = itemConfig.EffectId,
                    Params = new Dictionary<string, string> { { "value", (itemConfig.EffectId * 10).ToString() } }
                });
            }
        }

        public void IncrementInventoryRevision(int userId)
        {
            var inventory = GetOrCreateInventory(userId);
            inventory.Revision++;
        }

        #endregion

        #region Mail

        private MailListData GetOrCreateMails(int userId)
        {
            if (!_mails.TryGetValue(userId, out var mails))
            {
                mails = new MailListData { Mails = new List<MailData>(), UnreadCount = 0 };
                _mails[userId] = mails;
            }
            return mails;
        }

        public MailListData GetMails(int userId)
        {
            var mails = GetOrCreateMails(userId);
            var unreadBroadcasts = GetUnreadBroadcastMails(userId);
            foreach (var b in unreadBroadcasts)
            {
                mails.Mails.Insert(0, ToMailData(b));
            }
            return CloneMails(mails);
        }

        public MailData GetMail(int userId, string mailId)
        {
            var mails = GetOrCreateMails(userId);
            return mails.Mails?.FirstOrDefault(x => x.MailId == mailId);
        }

        public void MarkMailRead(int userId, string mailId)
        {
            var mails = GetOrCreateMails(userId);
            var mail = mails.Mails?.FirstOrDefault(x => x.MailId == mailId);
            if (mail != null)
            {
                mail.IsRead = true;
                mails.UnreadCount = mails.Mails.Count(x => !x.IsRead);
            }
            else
            {
                var broadcast = _broadcastMails.FirstOrDefault(x => x.MailId == mailId);
                if (broadcast != null)
                {
                    if (!_readBroadcastMailIds.ContainsKey(userId))
                        _readBroadcastMailIds[userId] = new List<string>();
                    if (!_readBroadcastMailIds[userId].Contains(mailId))
                        _readBroadcastMailIds[userId].Add(mailId);
                }
            }
        }

        public bool CanReceiveAttachment(int userId, string mailId)
        {
            var mails = GetOrCreateMails(userId);
            var mail = mails.Mails?.FirstOrDefault(x => x.MailId == mailId);
            if (mail == null || mail.IsReceived || mail.Attachments == null || mail.Attachments.Count == 0)
                return false;

            var inventory = GetOrCreateInventory(userId);
            var items = inventory.Items?.ToList() ?? new List<ItemData>();

            foreach (var attachment in mail.Attachments)
            {
                var maxStack = GetMaxStack(attachment.ItemId);
                int remaining = attachment.Count;

                for (int i = 0; i < items.Count && remaining > 0; i++)
                {
                    var item = items[i];
                    if (item.ItemId == attachment.ItemId && item.Count < maxStack)
                    {
                        remaining -= Math.Min(maxStack - item.Count, remaining);
                    }
                }

                while (remaining > 0)
                {
                    if (items.Count >= inventory.MaxSlots)
                        return false;
                    items.Add(new ItemData());
                    remaining -= maxStack;
                }
            }
            return true;
        }

        public void ReceiveMailAttachment(int userId, string mailId, out List<ItemData> changedItems)
        {
            changedItems = new List<ItemData>();
            var mails = GetOrCreateMails(userId);
            var mail = mails.Mails?.FirstOrDefault(x => x.MailId == mailId);
            if (mail == null || mail.IsReceived || mail.Attachments == null || mail.Attachments.Count == 0)
                return;

            var inventory = GetOrCreateInventory(userId);
            var items = inventory.Items?.ToList() ?? new List<ItemData>();

            foreach (var attachment in mail.Attachments)
            {
                var maxStack = GetMaxStack(attachment.ItemId);
                int remaining = attachment.Count;

                for (int i = 0; i < items.Count && remaining > 0; i++)
                {
                    var item = items[i];
                    if (item.ItemId == attachment.ItemId && item.Count < maxStack)
                    {
                        int canAdd = maxStack - item.Count;
                        int add = Math.Min(canAdd, remaining);
                        item.Count += add;
                        remaining -= add;

                        if (!changedItems.Contains(item))
                            changedItems.Add(item);
                    }
                }

                while (remaining > 0)
                {
                    var newItem = new ItemData
                    {
                        Uid = $"{attachment.ItemId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{UnityEngine.Random.Range(1000, 9999)}",
                        ItemId = attachment.ItemId,
                        Count = Math.Min(remaining, maxStack)
                    };
                    items.Add(newItem);
                    changedItems.Add(newItem);
                    remaining -= newItem.Count;
                }
            }

            inventory.Items = items.ToArray();
            mail.IsReceived = true;

            CleanupMails(userId, MAIL_MAX_COUNT);
        }

        public void DeleteMail(int userId, string mailId)
        {
            var mails = GetOrCreateMails(userId);
            var mail = mails.Mails?.FirstOrDefault(x => x.MailId == mailId);
            if (mail != null)
            {
                mails.Mails.Remove(mail);
                mails.UnreadCount = mails.Mails.Count(x => !x.IsRead);
            }
        }

        public void AddMail(int userId, MailData mail)
        {
            var mails = GetOrCreateMails(userId);
            if (mails.Mails == null)
                mails.Mails = new List<MailData>();

            mails.Mails.Insert(0, mail);
            mails.UnreadCount = mails.Mails.Count(x => !x.IsRead);

            CleanupMails(userId, MAIL_MAX_COUNT);
        }

        public void CleanupMails(int userId, int maxCount)
        {
            var mails = GetOrCreateMails(userId);
            if (mails.Mails == null) return;

            while (mails.Mails.Count > maxCount)
            {
                var oldest = mails.Mails.OrderBy(x => x.CreateTime).First();
                mails.Mails.Remove(oldest);
            }
            mails.UnreadCount = mails.Mails.Count(x => !x.IsRead);
        }

        #endregion

        #region Broadcast

        private List<BroadcastMailData> GetUnreadBroadcastMails(int userId)
        {
            if (!_readBroadcastMailIds.TryGetValue(userId, out var readIds))
                return _broadcastMails.ToList();

            return _broadcastMails
                .Where(b => !readIds.Contains(b.MailId))
                .ToList();
        }

        private MailData ToMailData(BroadcastMailData broadcast)
        {
            return new MailData
            {
                MailId = broadcast.MailId,
                Title = broadcast.Title,
                Content = broadcast.Content,
                Sender = broadcast.Sender,
                CreateTime = broadcast.CreateTime,
                IsRead = false,
                IsReceived = false,
                Attachments = broadcast.Attachments
            };
        }

        public void BroadcastMail(BroadcastMailData mail)
        {
            _broadcastMails.Add(mail);
        }

        #endregion

        #region Shop

        public void CheckDailyReset()
        {
            var today = DateTimeOffset.UtcNow.Date;
            var todayTimestamp = new DateTimeOffset(today).ToUnixTimeMilliseconds();
            if (_lastDailyResetTime < todayTimestamp)
            {
                _purchaseHistory.Clear();
                _shopRefreshCount = 0;
                _lastDailyResetTime = todayTimestamp;
                Debug.Log($"[LocalDatabase] 每日限购和刷新次数已重置");
            }
        }

        public int GetShopRefreshCount() => _shopRefreshCount;

        public bool CanRefreshShop() => _shopRefreshCount < ShopController.DAILY_REFRESH_LIMIT;

        public void IncrementRefreshCount() => _shopRefreshCount++;

        public Dictionary<int, int> GetPurchaseHistory(int userId)
        {
            return _purchaseHistory.GetValueOrDefault(userId) ?? new Dictionary<int, int>();
        }

        public bool CanPurchase(int userId, int shopItemId, int count, ShopItemConfig config)
        {
            if (config == null || config.LimitCount <= 0) return true;

            CheckDailyReset();
            var history = GetPurchaseHistory(userId);
            int purchased = history.GetValueOrDefault(shopItemId, 0);
            return (purchased + count) <= config.LimitCount;
        }

        public void RecordPurchase(int userId, int shopItemId, int count)
        {
            if (!_purchaseHistory.ContainsKey(userId))
                _purchaseHistory[userId] = new Dictionary<int, int>();

            var history = _purchaseHistory[userId];
            history[shopItemId] = history.GetValueOrDefault(shopItemId, 0) + count;
        }

        public ShopListData GetShopItems(int userId, string shopType)
        {
            CheckDailyReset();

            var history = GetPurchaseHistory(userId);
            var configs = _configSystem?.GetSheet<ShopItemConfig>()?.All()?.ToList() ?? new List<ShopItemConfig>();

            List<ShopItemData> items;
            if (shopType == ShopType.Fixed)
            {
                items = ShopController.GenerateFixedShop(userId, configs, history);
            }
            else
            {
                items = ShopController.GenerateRandomShop(userId, _shopRefreshCount, configs, history);
            }

            return new ShopListData
            {
                ShopType = shopType,
                Items = items,
                RefreshCount = _shopRefreshCount,
                MaxRefreshCount = ShopController.DAILY_REFRESH_LIMIT,
                CanRefresh = CanRefreshShop(),
                RefreshTime = _lastDailyResetTime
            };
        }

        public ShopListData RefreshShop(int userId, string shopType)
        {
            CheckDailyReset();

            if (!CanRefreshShop())
                return null;

            IncrementRefreshCount();
            return GetShopItems(userId, shopType);
        }

        public bool PurchaseItem(int userId, int shopItemId, int count, ShopItemConfig config,
            out PlayerData updatedPlayer, out InventoryData updatedInventory, out string errorMsg)
        {
            updatedPlayer = null;
            updatedInventory = null;
            errorMsg = null;

            if (config == null)
            {
                errorMsg = "商品不存在";
                return false;
            }

            if (!CanPurchase(userId, shopItemId, count, config))
            {
                errorMsg = "已达到限购上限";
                return false;
            }

            var player = _players.GetValueOrDefault(userId);
            if (player == null)
            {
                errorMsg = "玩家不存在";
                return false;
            }

            int totalPrice = config.Price * count;
            if (config.PriceType == CurrencyType.Gold)
            {
                if (player.Gold < totalPrice)
                {
                    errorMsg = "金币不足";
                    return false;
                }
                player.Gold -= totalPrice;
            }
            else if (config.PriceType == CurrencyType.Diamond)
            {
                if (player.Diamond < totalPrice)
                {
                    errorMsg = "钻石不足";
                    return false;
                }
                player.Diamond -= totalPrice;
            }

            AddItem(userId, config.ItemId, config.ItemCount * count, out _, out _);
            RecordPurchase(userId, shopItemId, count);

            updatedPlayer = ClonePlayer(player);
            updatedInventory = GetInventory(userId);

            return true;
        }

        #endregion

        #region Helpers

        private int GetMaxStack(int itemId)
        {
            var config = GetItemConfig(itemId);
            return config?.MaxStack ?? 99;
        }

        private ItemConfig GetItemConfig(int itemId)
        {
            if (_configSystem != null)
                return _configSystem.Get<ItemConfig>(itemId);
            return null;
        }

        private PlayerData ClonePlayer(PlayerData original)
        {
            if (original == null) return null;
            return new PlayerData
            {
                Username = original.Username,
                Diamond = original.Diamond,
                Gold = original.Gold,
                Exp = original.Exp,
                Energy = original.Energy,
                LastEnergyRecoverTime = original.LastEnergyRecoverTime,
                ServerTime = original.ServerTime
            };
        }

        private InventoryData CloneInventory(InventoryData original)
        {
            if (original == null) return null;
            return new InventoryData
            {
                Items = original.Items?.Select(CloneItem).ToArray(),
                MaxSlots = original.MaxSlots,
                Revision = original.Revision
            };
        }

        private ItemData CloneItem(ItemData original)
        {
            if (original == null) return null;
            return new ItemData
            {
                Uid = original.Uid,
                ItemId = original.ItemId,
                Count = original.Count
            };
        }

        private MailListData CloneMails(MailListData original)
        {
            if (original == null) return null;
            return new MailListData
            {
                Mails = original.Mails?.Select(CloneMail).ToList(),
                UnreadCount = original.UnreadCount
            };
        }

        private MailData CloneMail(MailData original)
        {
            if (original == null) return null;
            return new MailData
            {
                MailId = original.MailId,
                Title = original.Title,
                Content = original.Content,
                Sender = original.Sender,
                CreateTime = original.CreateTime,
                IsRead = original.IsRead,
                IsReceived = original.IsReceived,
                Attachments = original.Attachments
            };
        }

        #endregion
    }

    #region SaveData

    public class LocalDatabaseData
    {
        public Dictionary<int, PlayerData> Players { get; set; }
        public Dictionary<int, InventoryData> Inventories { get; set; }
        public Dictionary<int, MailListData> Mails { get; set; }
        public List<BroadcastMailData> BroadcastMails { get; set; }
        public Dictionary<int, List<string>> ReadBroadcastMailIds { get; set; }
        public Dictionary<int, Dictionary<int, int>> PurchaseHistory { get; set; }
        public long LastDailyResetTime { get; set; }
        public int ShopRefreshCount { get; set; }
    }

    #endregion
}
