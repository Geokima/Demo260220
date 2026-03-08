using System;
using System.Collections.Generic;
using System.Linq;
using Game.Config;
using Game.Consts;
using Game.DTOs;

namespace Game.Gateways
{
    public static class ShopController
    {
        public const int DAILY_REFRESH_LIMIT = 99;
        public const int RANDOM_SHOP_ITEM_COUNT = 6;

        public static ShopRefreshResponse HandleRefresh(ServerContext ctx, ShopRefreshRequest req)
        {
            var shopType = req?.ShopType ?? ShopType.Random;
            
            // 规范：只有随机商店可以手动刷新
            if (shopType != ShopType.Random)
            {
                return new ShopRefreshResponse { Code = 1, Msg = "Only random shop can be refreshed" };
            }

            if (!ctx.Db.CanRefreshShop(ctx.UserId))
                return new ShopRefreshResponse { Code = (int)ErrorCode.ShopRefreshLimitReached, Msg = "Refresh limit reached" };

            ctx.Db.IncrementRefreshCount(ctx.UserId);
            var data = ctx.Db.GetShopItems(ctx.UserId, shopType);
            
            return new ShopRefreshResponse { Code = 0, Data = data };
        }

        public static ShopListResponse HandleGetShopList(ServerContext ctx, ShopListRequest req)
        {
            var shopType = req?.ShopType ?? ShopType.Fixed;
            var data = ctx.Db.GetShopItems(ctx.UserId, shopType);
            return new ShopListResponse { Code = 0, Data = data };
        }

        public static ShopBuyResponse HandleBuy(ServerContext ctx, ShopBuyRequest req)
        {
            if (req == null || req.ShopItemId <= 0)
                return new ShopBuyResponse { Code = (int)ErrorCode.InvalidParams, Msg = "Invalid request" };

            if (req.Count <= 0) req.Count = 1;

            var config = ctx.Configs.Get<ShopItemConfig>(req.ShopItemId);
            if (config == null)
                return new ShopBuyResponse { Code = (int)ErrorCode.ShopItemNotFound, Msg = "Item not found" };

            // 1. 限购校验
            if (!ctx.Db.CanPurchase(ctx.UserId, req.ShopItemId, req.Count, config))
                return new ShopBuyResponse { Code = (int)ErrorCode.ShopLimitReached, Msg = "Purchase limit reached" };

            // 2. 货币校验
            var player = ctx.Db.GetPlayer(ctx.UserId);
            int totalPrice = config.Price * req.Count;
            if (config.PriceType == CurrencyType.Gold)
            {
                if (player.Gold < totalPrice)
                    return new ShopBuyResponse { Code = (int)ErrorCode.InsufficientGold, Msg = "Gold not enough" };
                player.Gold -= totalPrice;
            }
            else if (config.PriceType == CurrencyType.Diamond)
            {
                if (player.Diamond < totalPrice)
                    return new ShopBuyResponse { Code = (int)ErrorCode.InsufficientDiamond, Msg = "Diamond not enough" };
                player.Diamond -= totalPrice;
            }

            // 3. 执行变更
            ctx.Db.UpdatePlayer(ctx.UserId, player);
            
            var buyRewards = new List<ObtainItem> { new ObtainItem { ItemId = config.ItemId, Count = config.ItemCount * req.Count } };
            ctx.Db.ApplyObtainItems(ctx.UserId, buyRewards, out var obtainResult);
            ctx.Db.RecordPurchase(ctx.UserId, req.ShopItemId, req.Count, config.ResetType);

            var updatedPlayer = ctx.Db.GetPlayer(ctx.UserId);
            var updatedInventory = ctx.Db.GetInventory(ctx.UserId);

            // 4. 推送
            if (obtainResult.PlayerDataChanged)
                ctx.DirectPushAction?.Invoke(ctx.UserId, NetworkMsgType.PlayerSync, updatedPlayer);
            
            if (obtainResult.RealChangedItems.Count > 0)
            {
                ctx.DirectPushAction?.Invoke(ctx.UserId, NetworkMsgType.InventoryUpdate, new InventorySyncData
                {
                    ChangedItems = obtainResult.RealChangedItems,
                    Reason = InventorySyncReason.BUY,
                    Revision = updatedInventory.Revision
                });
            }

            return new ShopBuyResponse
            {
                Code = 0,
                Data = new ShopBuyResponseData
                {
                    PlayerSync = updatedPlayer,
                    InventorySync = new InventorySyncData
                    {
                        ChangedItems = obtainResult.RealChangedItems,
                        Reason = InventorySyncReason.BUY,
                        Revision = updatedInventory.Revision
                    },
                    ShopSync = ctx.Db.GetShopItems(ctx.UserId, config.ShopType)
                }
            };
        }

        public static List<ShopItemData> GenerateFixedShop(int userId, string shopType, List<ShopItemConfig> configs, Dictionary<int, int> dailyHistory, Dictionary<int, int> permanentHistory)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // 只要 ShopType 匹配即可支持各种固定商店（如 Fixed, Event1, EventSpecial 等）
            var validConfigs = configs
                .Where(c => c.ShopType == shopType && c.StartTime <= now && c.EndTime > now)
                .OrderBy(c => c.Id)
                .ToList();

            var result = new List<ShopItemData>();
            foreach (var config in validConfigs)
            {
                var history = config.ResetType == 1 ? dailyHistory : permanentHistory;
                int purchased = history.GetValueOrDefault(config.Id, 0);
                result.Add(ToShopItemData(config, purchased));
            }
            return result;
        }

        public static List<ShopItemData> GenerateRandomShop(int userId, int refreshCount, List<ShopItemConfig> configs, Dictionary<int, int> dailyHistory, Dictionary<int, int> permanentHistory)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var randomConfigs = configs
                .Where(c => c.ShopType == ShopType.Random && c.Weight > 0 && c.StartTime <= now && c.EndTime > now)
                .OrderBy(c => c.Id)
                .ToList();

            if (randomConfigs.Count == 0)
                return new List<ShopItemData>();

            int seed = CalculateSeed(userId, refreshCount);
            var indices = GenerateRandomIndices(seed, randomConfigs.Count);
            var selectedIndices = indices.Take(RANDOM_SHOP_ITEM_COUNT).ToList();

            var result = new List<ShopItemData>();
            foreach (var idx in selectedIndices)
            {
                var config = randomConfigs[idx];
                var history = config.ResetType == 1 ? dailyHistory : permanentHistory;
                int purchased = history.GetValueOrDefault(config.Id, 0);
                result.Add(ToShopItemData(config, purchased));
            }
            return result;
        }

        private static int CalculateSeed(int userId, int refreshCount)
        {
            var date = int.Parse(DateTimeOffset.UtcNow.ToString("yyyyMMdd"));
            return userId ^ date ^ refreshCount;
        }

        private static List<int> GenerateRandomIndices(int seed, int count)
        {
            var indices = Enumerable.Range(0, count).ToList();
            var random = new Random(seed);
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }
            return indices;
        }

        private static ShopItemData ToShopItemData(ShopItemConfig config, int purchased)
        {
            return new ShopItemData
            {
                ShopItemId = config.Id,
                ItemId = config.ItemId,
                ItemCount = config.ItemCount,
                PriceType = config.PriceType,
                OriginalPrice = config.OriginalPrice,
                Price = config.Price,
                Discount = config.Discount,
                LimitCount = config.LimitCount,
                PurchasedCount = purchased,
                CanBuy = config.LimitCount <= 0 || purchased < config.LimitCount
            };
        }
    }
}
