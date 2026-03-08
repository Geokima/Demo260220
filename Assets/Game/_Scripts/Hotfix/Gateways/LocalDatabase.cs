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

        private Dictionary<int, PlayerData> _players = new();
        private Dictionary<int, InventoryData> _inventories = new();
        private Dictionary<int, MailListData> _mails = new();
        private List<BroadcastMailData> _broadcastMails = new();
        private Dictionary<int, List<string>> _readBroadcastMailIds = new();
        private Dictionary<int, Dictionary<int, int>> _dailyPurchaseHistory = new();
        private Dictionary<int, Dictionary<int, int>> _permanentPurchaseHistory = new();
        private Dictionary<int, MissionListData> _missions = new();
        
        private Dictionary<int, long> _userLastResetTimes = new();
        private Dictionary<int, long> _userLastWeeklyResetTimes = new();
        private Dictionary<int, int> _userShopRefreshCounts = new();
        private Dictionary<int, Dictionary<int, int>> _weeklyPurchaseHistory = new();
        
        private int _nextUserId = 1;

        #endregion

        public LocalDatabase()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "local_db.json");
        }

        public void Init(IConfigSystem configSystem)
        {
            _configSystem = configSystem;
            Load();
        }

        private void Load()
        {
            if (!File.Exists(_savePath)) return;

            try
            {
                var json = File.ReadAllText(_savePath);
                var jo = JObject.Parse(json);
                if (jo != null)
                {
                    _players = jo["Players"]?.ToObject<Dictionary<int, PlayerData>>() ?? new Dictionary<int, PlayerData>();
                    _inventories = jo["Inventories"]?.ToObject<Dictionary<int, InventoryData>>() ?? new Dictionary<int, InventoryData>();
                    _mails = jo["Mails"]?.ToObject<Dictionary<int, MailListData>>() ?? new Dictionary<int, MailListData>();
                    _broadcastMails = jo["BroadcastMails"]?.ToObject<List<BroadcastMailData>>() ?? new List<BroadcastMailData>();
                    _readBroadcastMailIds = jo["ReadBroadcastMailIds"]?.ToObject<Dictionary<int, List<string>>>() ?? new Dictionary<int, List<string>>();
                    _dailyPurchaseHistory = jo["DailyPurchaseHistory"]?.ToObject<Dictionary<int, Dictionary<int, int>>>() ?? new Dictionary<int, Dictionary<int, int>>();
                    _weeklyPurchaseHistory = jo["WeeklyPurchaseHistory"]?.ToObject<Dictionary<int, Dictionary<int, int>>>() ?? new Dictionary<int, Dictionary<int, int>>();
                    _permanentPurchaseHistory = jo["PermanentPurchaseHistory"]?.ToObject<Dictionary<int, Dictionary<int, int>>>() ?? new Dictionary<int, Dictionary<int, int>>();
                    _missions = jo["Missions"]?.ToObject<Dictionary<int, MissionListData>>() ?? new Dictionary<int, MissionListData>();
                    
                    _userLastResetTimes = jo["UserLastResetTimes"]?.ToObject<Dictionary<int, long>>() ?? new Dictionary<int, long>();
                    _userLastWeeklyResetTimes = jo["UserLastWeeklyResetTimes"]?.ToObject<Dictionary<int, long>>() ?? new Dictionary<int, long>();
                    _userShopRefreshCounts = jo["UserShopRefreshCounts"]?.ToObject<Dictionary<int, int>>() ?? new Dictionary<int, int>();
                    
                    _nextUserId = _players.Keys.DefaultIfEmpty(0).Max() + 1;
                }
            }
            catch (Exception ex)
            {
                Debug.Log($"[LocalDatabase] Load Error: {ex.Message}");
            }
        }

        public void Save()
        {
            try
            {
                var data = new LocalDatabaseData
                {
                    Players = _players,
                    Inventories = _inventories,
                    Mails = _mails,
                    BroadcastMails = _broadcastMails,
                    ReadBroadcastMailIds = _readBroadcastMailIds,
                    DailyPurchaseHistory = _dailyPurchaseHistory,
                    WeeklyPurchaseHistory = _weeklyPurchaseHistory,
                    PermanentPurchaseHistory = _permanentPurchaseHistory,
                    Missions = _missions,
                    UserLastResetTimes = _userLastResetTimes,
                    UserLastWeeklyResetTimes = _userLastWeeklyResetTimes,
                    UserShopRefreshCounts = _userShopRefreshCounts
                };
                File.WriteAllText(_savePath, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Debug.Log($"[LocalDatabase] Save Error: {ex.Message}");
            }
        }

        #region Player
        public PlayerData GetPlayer(int userId) => _players.GetValueOrDefault(userId);
        public void UpdatePlayer(int userId, PlayerData data) => _players[userId] = data;

        public int GetUserIdByUsername(string username)
        {
            foreach (var kvp in _players)
            {
                if (kvp.Value != null && kvp.Value.Username == username)
                    return kvp.Key;
            }
            return 0;
        }

        public bool HasUser(int userId) => _players.ContainsKey(userId);

        public int GetNextUserId(string username)
        {
            int userId = _nextUserId++;
            _players[userId] = new PlayerData { Username = username, Gold = 1000, Diamond = 100, Energy = 100 };
            return userId;
        }
        #endregion

        #region Inventory
        public InventoryData GetInventory(int userId) => _inventories.GetValueOrDefault(userId) ?? new InventoryData { Items = Array.Empty<ItemData>(), Revision = 1, MaxSlots = 100 };
        
        public void IncrementInventoryRevision(int userId)
        {
            var inv = GetInventory(userId);
            inv.Revision++;
            _inventories[userId] = inv;
        }

        public bool AddItem(int userId, int itemId, int count, out ItemData updatedItem, out List<ItemData> allChanged, long expireTime = 0)
        {
            var inv = GetInventory(userId);
            var items = inv.Items?.ToList() ?? new List<ItemData>();
            
            // 逻辑：如果物品有过期时间，或者是不堆叠的，通常不能简单的合并。
            // 但为了简化模拟，如果 itemId 和 expireTime 都一致，我们视为可堆叠。
            var item = items.FirstOrDefault(i => i.ItemId == itemId && i.ExpireTime == expireTime);
            if (item == null)
            {
                item = new ItemData { Uid = Guid.NewGuid().ToString(), ItemId = itemId, Count = count, ExpireTime = expireTime };
                items.Add(item);
            }
            else
            {
                item.Count += count;
            }
            inv.Items = items.ToArray();
            inv.Revision++;
            _inventories[userId] = inv;
            updatedItem = item;
            allChanged = new List<ItemData> { item };
            return true;
        }

        public bool RemoveItem(int userId, string uid, int count, out ItemData updatedItem, out string removedUid)
        {
            var inv = GetInventory(userId);
            var items = inv.Items?.ToList() ?? new List<ItemData>();
            var item = items.FirstOrDefault(i => i.Uid == uid);
            removedUid = null;
            if (item == null || item.Count < count)
            {
                updatedItem = null;
                return false;
            }
            item.Count -= count;
            if (item.Count <= 0)
            {
                items.Remove(item);
                removedUid = uid;
                updatedItem = null;
            }
            else
            {
                updatedItem = item;
            }
            inv.Items = items.ToArray();
            inv.Revision++;
            _inventories[userId] = inv;
            return true;
        }

        public bool UseItem(int userId, string uid, int count, out ItemData updatedItem, out List<ItemEffect> effects)
        {
            effects = new List<ItemEffect>();
            bool success = RemoveItem(userId, uid, count, out updatedItem, out _);
            return success;
        }
        #endregion

        #region Mail
        public MailListData GetMails(int userId) => _mails.GetValueOrDefault(userId) ?? new MailListData { Mails = new List<MailData>(), Revision = 1 };
        public void UpdateMails(int userId, MailListData data) => _mails[userId] = data;

        public MailData GetMail(int userId, string mailId)
        {
            var list = GetMails(userId);
            return list.Mails?.FirstOrDefault(m => m.MailId == mailId);
        }

        public void AddMail(int userId, MailData mail)
        {
            var list = GetMails(userId);
            if (list.Mails == null) list.Mails = new List<MailData>();
            list.Mails.Add(mail);
            list.Revision++;
            _mails[userId] = list;
        }

        public void MarkMailRead(int userId, string mailId)
        {
            var mail = GetMail(userId, mailId);
            if (mail != null && !mail.IsRead)
            {
                mail.IsRead = true;
                var list = GetMails(userId);
                list.Revision++;
                _mails[userId] = list;
            }
        }

        public void MarkMailReceived(int userId, string mailId)
        {
            var mail = GetMail(userId, mailId);
            if (mail != null && !mail.IsReceived)
            {
                mail.IsReceived = true;
                var list = GetMails(userId);
                list.Revision++;
                _mails[userId] = list;
            }
        }

        public void DeleteMail(int userId, string mailId, out MailSyncData syncData)
        {
            var list = GetMails(userId);
            var mail = list.Mails?.FirstOrDefault(m => m.MailId == mailId);
            if (mail != null)
            {
                list.Mails.Remove(mail);
                list.Revision++;
                _mails[userId] = list;
            }
            syncData = new MailSyncData { RemovedIds = new List<string> { mailId }, Revision = list.Revision };
        }

        public bool ApplyObtainItems(int userId, List<ObtainItem> rewards, out ObtainItemResult result)
        {
            result = new ObtainItemResult { RealChangedItems = new List<ItemData>(), ObtainedItems = new List<ObtainItem>() };
            var player = GetPlayer(userId);
            if (player == null) return false;
            foreach (var reward in rewards)
            {
                if (reward.Type == ObtainType.Currency || reward.ItemId <= CurrencyId.Diamond)
                {
                    if (reward.ItemId == CurrencyId.Gold || reward.Type == ObtainType.Gold) player.Gold += reward.Count;
                    else if (reward.ItemId == CurrencyId.Diamond || reward.Type == ObtainType.Diamond) player.Diamond += reward.Count;
                    result.PlayerDataChanged = true;
                    result.ObtainedItems.Add(reward);
                }
                else
                {
                    if (AddItem(userId, reward.ItemId, reward.Count, out var item, out _, reward.ExpireTime))
                    {
                        result.RealChangedItems.Add(item);
                        result.ObtainedItems.Add(reward);
                    }
                }
            }
            if (result.PlayerDataChanged) UpdatePlayer(userId, player);
            result.UpdatedPlayer = player;
            return true;
        }
        #endregion

        #region Shop
        public void CheckAllStatusReset(int userId)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var logicDay = GetLogicDayTimestamp(nowMs);
            var logicWeek = GetLogicWeeklyTimestamp(nowMs);

            // 1. 每日重置
            var lastDailyReset = _userLastResetTimes.GetValueOrDefault(userId, 0);
            if (lastDailyReset < logicDay)
            {
                DoResetDaily(userId, logicDay);
            }

            // 2. 每周重置
            var lastWeeklyReset = _userLastWeeklyResetTimes.GetValueOrDefault(userId, 0);
            if (lastWeeklyReset < logicWeek)
            {
                DoResetWeekly(userId, logicWeek);
            }

            // 3. 物品过期检查 (每次触发重置检查时顺便扫描)
            HandleItemExpiration(userId);
        }

        private long GetLogicDayTimestamp(long timeMs)
        {
            // 减去 4 小时偏移量
            var offsetTime = DateTimeOffset.FromUnixTimeMilliseconds(timeMs).AddHours(-GameTimeConsts.DailyResetHour);
            return new DateTimeOffset(offsetTime.Date).ToUnixTimeMilliseconds();
        }

        private long GetLogicWeeklyTimestamp(long timeMs)
        {
            var offsetTime = DateTimeOffset.FromUnixTimeMilliseconds(timeMs).AddHours(-GameTimeConsts.DailyResetHour);
            // 计算本周一 0 点 (DayOfWeek: Sunday=0, Monday=1...)
            int diff = (7 + (int)offsetTime.DayOfWeek - (int)GameTimeConsts.WeeklyResetDay) % 7;
            return new DateTimeOffset(offsetTime.Date.AddDays(-diff)).ToUnixTimeMilliseconds();
        }

        private void DoResetDaily(int userId, long logicDay)
        {
            _dailyPurchaseHistory.Remove(userId);
            _userShopRefreshCounts[userId] = 0;
            _userLastResetTimes[userId] = logicDay;
            
            // 如果是在线推送点，这里通常可以触发一个事件，但目前由 Gateway 统一处理
        }

        private void DoResetWeekly(int userId, long logicWeek)
        {
            _weeklyPurchaseHistory.Remove(userId);
            _userLastWeeklyResetTimes[userId] = logicWeek;
            
            // 重置周任务 (如果以后有具体字段，在这里处理)
            var missions = GetMissions(userId);
            bool changed = false;
            if (missions.Missions != null)
            {
                foreach (var m in missions.Missions)
                {
                    if (m.Type == "weekly") // 假设类型为 weekly
                    {
                        m.Status = MissionStatus.InProgress;
                        m.CurrentProgress = 0;
                        changed = true;
                    }
                }
            }
            if (changed) UpdateMissions(userId, missions);
        }

        private void HandleItemExpiration(int userId)
        {
            var inv = GetInventory(userId);
            if (inv.Items == null || inv.Items.Length == 0) return;

            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var itemsList = inv.Items.ToList();
            var removedUids = new List<string>();

            for (int i = itemsList.Count - 1; i >= 0; i--)
            {
                var item = itemsList[i];
                if (item.ExpireTime > 0 && item.ExpireTime <= nowMs)
                {
                    removedUids.Add(item.Uid);
                    itemsList.RemoveAt(i);
                }
            }

            if (removedUids.Count > 0)
            {
                inv.Items = itemsList.ToArray();
                inv.Revision++;
                _inventories[userId] = inv;

                // 强制推送给前端
                // 注意：在模拟环境下，如果在这里直接 Push 需要上下文，目前我们可以返回通知或在 Gateway 层处理。
            }
        }

        public bool CanRefreshShop(int userId) => _userShopRefreshCounts.GetValueOrDefault(userId, 0) < ShopController.DAILY_REFRESH_LIMIT;
        public void IncrementRefreshCount(int userId) => _userShopRefreshCounts[userId] = _userShopRefreshCounts.GetValueOrDefault(userId, 0) + 1;

        public bool CanPurchase(int userId, int shopItemId, int count, ShopItemConfig config)
        {
            if (config == null || config.LimitCount <= 0) return true;
            CheckAllStatusReset(userId);
            
            Dictionary<int, Dictionary<int, int>> dict;
            if (config.ResetType == ResetType.Daily) dict = _dailyPurchaseHistory;
            else if (config.ResetType == ResetType.Weekly) dict = _weeklyPurchaseHistory;
            else dict = _permanentPurchaseHistory;

            var history = dict.GetValueOrDefault(userId) ?? new Dictionary<int, int>();
            return (history.GetValueOrDefault(shopItemId, 0) + count) <= config.LimitCount;
        }

        public void RecordPurchase(int userId, int shopItemId, int count, int resetType)
        {
            Dictionary<int, Dictionary<int, int>> dict;
            if (resetType == ResetType.Daily) dict = _dailyPurchaseHistory;
            else if (resetType == ResetType.Weekly) dict = _weeklyPurchaseHistory;
            else dict = _permanentPurchaseHistory;

            if (!dict.ContainsKey(userId)) dict[userId] = new Dictionary<int, int>();
            var history = dict[userId];
            history[shopItemId] = history.GetValueOrDefault(shopItemId, 0) + count;
        }

        public ShopListData GetShopItems(int userId, string shopType)
        {
            CheckAllStatusReset(userId);
            var daily = _dailyPurchaseHistory.GetValueOrDefault(userId) ?? new Dictionary<int, int>();
            var permanent = _permanentPurchaseHistory.GetValueOrDefault(userId) ?? new Dictionary<int, int>();
            var configs = _configSystem?.GetSheet<ShopItemConfig>()?.All()?.ToList() ?? new List<ShopItemConfig>();

            int refreshCount = _userShopRefreshCounts.GetValueOrDefault(userId, 0);
            List<ShopItemData> items;
            if (shopType == ShopType.Random)
                items = ShopController.GenerateRandomShop(userId, refreshCount, configs, daily, permanent);
            else
                items = ShopController.GenerateFixedShop(userId, shopType, configs, daily, permanent);

            return new ShopListData { 
                ShopType = shopType, 
                Items = items, 
                RefreshCount = refreshCount, 
                MaxRefreshCount = ShopController.DAILY_REFRESH_LIMIT, 
                CanRefresh = (shopType == ShopType.Random && CanRefreshShop(userId)), 
                RefreshTime = _userLastResetTimes.GetValueOrDefault(userId, 0)
            };
        }
        #endregion

        #region Mission
        public MissionListData GetMissions(int userId) => _missions.GetValueOrDefault(userId) ?? new MissionListData { Missions = Array.Empty<MissionData>(), Revision = 1 };
        public void UpdateMissions(int userId, MissionListData data) => _missions[userId] = data;
        #endregion

        #region Helpers
        public void BroadcastMail(BroadcastMailData mail) => _broadcastMails.Add(mail);
        public List<BroadcastMailData> GetUnreadBroadcastMails(int userId)
        {
            var readIds = _readBroadcastMailIds.GetValueOrDefault(userId) ?? new List<string>();
            return _broadcastMails.Where(b => !readIds.Contains(b.MailId)).ToList();
        }
        #endregion
    }

    public class LocalDatabaseData
    {
        public Dictionary<int, PlayerData> Players { get; set; }
        public Dictionary<int, InventoryData> Inventories { get; set; }
        public Dictionary<int, MailListData> Mails { get; set; }
        public List<BroadcastMailData> BroadcastMails { get; set; }
        public Dictionary<int, List<string>> ReadBroadcastMailIds { get; set; }
        public Dictionary<int, Dictionary<int, int>> DailyPurchaseHistory { get; set; }
        public Dictionary<int, Dictionary<int, int>> WeeklyPurchaseHistory { get; set; }
        public Dictionary<int, Dictionary<int, int>> PermanentPurchaseHistory { get; set; }
        public Dictionary<int, MissionListData> Missions { get; set; }
        
        public Dictionary<int, long> UserLastResetTimes { get; set; }
        public Dictionary<int, long> UserLastWeeklyResetTimes { get; set; }
        public Dictionary<int, int> UserShopRefreshCounts { get; set; }
    }

    public class ObtainItemResult
    {
        public List<ItemData> RealChangedItems;
        public List<ObtainItem> ObtainedItems;
        public bool PlayerDataChanged;
        public PlayerData UpdatedPlayer;
    }
}
