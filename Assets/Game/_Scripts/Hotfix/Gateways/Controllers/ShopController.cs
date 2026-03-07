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
        public static int CalculateSeed(int userId, int refreshCount)
        {
            var date = int.Parse(DateTimeOffset.UtcNow.ToString("yyyyMMdd"));
            return userId ^ date ^ refreshCount;
        }

        public static List<int> GenerateRandomIndices(int seed, int count)
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

        public static List<ShopItemData> GenerateFixedShop(int userId, List<ShopItemConfig> configs, Dictionary<int, int> purchaseHistory)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var validConfigs = configs
                .Where(c => c.ShopType == ShopType.Fixed && c.StartTime <= now && c.EndTime > now)
                .OrderBy(c => c.Id)
                .ToList();

            var result = new List<ShopItemData>();
            foreach (var config in validConfigs)
            {
                int purchased = purchaseHistory.GetValueOrDefault(config.Id, 0);
                result.Add(ToShopItemData(config, purchased));
            }
            return result;
        }

        public static List<ShopItemData> GenerateRandomShop(int userId, int refreshCount, List<ShopItemConfig> configs, Dictionary<int, int> purchaseHistory)
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
                int purchased = purchaseHistory.GetValueOrDefault(config.Id, 0);
                result.Add(ToShopItemData(config, purchased));
            }
            return result;
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
