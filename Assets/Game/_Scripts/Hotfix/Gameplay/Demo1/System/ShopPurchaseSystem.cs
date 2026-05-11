using Framework;
using Framework.Modules.Config;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Gameplay.Demo1.System
{
    public enum ShopPurchaseResult
    {
        Success = 0,
        InvalidCard = 1,
        NotEnoughGold = 2,
        NoSpace = 3,
        NoRefreshLeft = 4
    }

    public enum ShopPurchaseOutcome
    {
        None = 0,
        AddedToActive = 1,
        AddedToBench = 2,
        MergedInActive = 3,
        MergedInBench = 4
    }

    public interface IShopPurchaseSystem : ISystem
    {
        ShopPurchaseResult TryBuy(CardModel card, out ShopPurchaseOutcome outcome);
        void OpenShop();
        bool RefreshShop();
        CardModel[] GetCurrentShopCards();
        int RefreshCost { get; }
        int RemainingRefreshCount { get; }
    }

    public class ShopPurchaseSystem : AbstractSystem, IShopPurchaseSystem
    {
        public ShopPurchaseResult TryBuy(CardModel card, out ShopPurchaseOutcome outcome)
        {
            outcome = ShopPurchaseOutcome.None;
            if (card == null)
                return ShopPurchaseResult.InvalidCard;

            var model = this.GetModel<Demo1Model>();
            if (model == null)
                return ShopPurchaseResult.InvalidCard;

            int cost = card.Price != null ? card.Price.Value : 0;
            if (model.Gold.Value < cost)
                return ShopPurchaseResult.NotEnoughGold;

            if (TryMerge(model.ActiveSlots, card, out var mergedCard))
            {
                model.Gold.Value -= cost;
                UpgradeRankOnce(mergedCard);
                mergedCard.ApplyPriceRule(isInShop: false);
                outcome = ShopPurchaseOutcome.MergedInActive;
                return ShopPurchaseResult.Success;
            }

            if (TryMerge(model.BenchCards, card, out mergedCard))
            {
                model.Gold.Value -= cost;
                UpgradeRankOnce(mergedCard);
                mergedCard.ApplyPriceRule(isInShop: false);
                outcome = ShopPurchaseOutcome.MergedInBench;
                return ShopPurchaseResult.Success;
            }

            int activeCapacity = model.MaxSlotCount != null ? model.MaxSlotCount.Value : Demo1Const.InitialSlots;
            activeCapacity = Clamp(activeCapacity, Demo1Const.InitialSlots, Demo1Const.MaxSlots);
            if (CanFit(model.ActiveSlots, activeCapacity, card))
            {
                model.Gold.Value -= cost;
                card.ApplyPriceRule(isInShop: false);
                model.ActiveSlots.Add(card);
                outcome = ShopPurchaseOutcome.AddedToActive;
                return ShopPurchaseResult.Success;
            }

            if (CanFit(model.BenchCards, Demo1Const.BenchMaxSlots, card))
            {
                model.Gold.Value -= cost;
                card.ApplyPriceRule(isInShop: false);
                model.BenchCards.Add(card);
                outcome = ShopPurchaseOutcome.AddedToBench;
                return ShopPurchaseResult.Success;
            }

            return ShopPurchaseResult.NoSpace;
        }

        private static bool CanFit(BindableList<CardModel> list, int capacity, CardModel incoming)
        {
            if (list == null || incoming == null)
                return false;

            int used = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                if (m == null)
                    continue;
                used += Clamp(m.Size, 1, 3);
            }

            int incomingSize = Clamp(incoming.Size, 1, 3);
            return used + incomingSize <= capacity;
        }

        private static bool TryMerge(BindableList<CardModel> list, CardModel incoming, out CardModel mergedCard)
        {
            mergedCard = null;
            if (list == null || incoming == null)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                var existing = list[i];
                if (existing == null)
                    continue;

                if (!IsSameCard(existing, incoming))
                    continue;

                if (existing.Rank.Value >= CardRank.Diamond)
                    continue;

                mergedCard = existing;
                return true;
            }

            return false;
        }

        private static bool IsSameCard(CardModel a, CardModel b)
        {
            if (a == null || b == null)
                return false;

            if (a.Rank.Value != b.Rank.Value)
                return false;

            if (a.Id != 0 && b.Id != 0)
                return a.Id == b.Id;

            if (!string.IsNullOrEmpty(a.Name) && !string.IsNullOrEmpty(b.Name))
                return a.Name == b.Name;

            return false;
        }

        private static void UpgradeRankOnce(CardModel card)
        {
            if (card == null)
                return;

            var currentRank = card.Rank.Value;
            if (currentRank >= CardRank.Diamond)
                return;

            var configSystem = Demo1Architecture.Instance.GetSystem<IConfigSystem>();
            var sheet = configSystem.GetSheet<Demo1CardConfig>();

            var nextRank = (CardRank)((int)currentRank + 1);
            var nextConfig = sheet.All().FirstOrDefault(c => c.Name == card.Name && GetCardRank(c.Rank) == nextRank);
            if (nextConfig == null)
                return;

            card.Bind(nextConfig);
            card.ApplyPriceRule(isInShop: false);
            card.CurrentCD.Value = card.MaxCD.Value;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private const int DefaultRefreshCost = 2;
        private const int DefaultMaxRefreshCount = 1;
        private const int ShopCardCount = 3;

        private int _remainingRefreshCount;
        private CardModel[] _currentCards = new CardModel[0];

        public int RefreshCost => DefaultRefreshCost;
        public int RemainingRefreshCount => _remainingRefreshCount;

        public void OpenShop()
        {
            _remainingRefreshCount = DefaultMaxRefreshCount;
            DrawRandomCards();
        }

        public bool RefreshShop()
        {
            if (_remainingRefreshCount <= 0)
                return false;

            var model = this.GetModel<Demo1Model>();
            if (model.Gold.Value < DefaultRefreshCost)
                return false;

            model.Gold.Value -= DefaultRefreshCost;
            _remainingRefreshCount--;
            DrawRandomCards();
            return true;
        }

        public CardModel[] GetCurrentShopCards()
        {
            return _currentCards.ToArray();
        }

        private void DrawRandomCards()
        {
            var configSystem = this.GetSystem<IConfigSystem>();
            var sheet = configSystem.GetSheet<Demo1CardConfig>();
            var allConfigs = sheet.All().ToList();

            if (allConfigs == null || allConfigs.Count == 0)
            {
                _currentCards = new CardModel[0];
                return;
            }

            var model = this.GetModel<Demo1Model>();
            int playerLevel = model?.Level?.Value ?? 1;

            var random = new Random();
            var selectedConfigs = new List<Demo1CardConfig>();

            for (int i = 0; i < ShopCardCount; i++)
            {
                var rank = RollCardRank(playerLevel, random);
                var candidates = allConfigs.Where(c => GetCardRank(c.Rank) == rank).ToList();
                if (candidates.Count > 0)
                {
                    selectedConfigs.Add(candidates[random.Next(candidates.Count)]);
                }
                else
                {
                    var bronzeCandidates = allConfigs.Where(c => GetCardRank(c.Rank) == CardRank.Bronze).ToList();
                    if (bronzeCandidates.Count > 0)
                    {
                        selectedConfigs.Add(bronzeCandidates[random.Next(bronzeCandidates.Count)]);
                    }
                }
            }

            _currentCards = selectedConfigs.Select(config =>
            {
                var card = new CardModel(config);
                card.ApplyPriceRule(isInShop: true);
                return card;
            }).ToArray();
        }

        private static readonly Dictionary<int, Dictionary<CardRank, int>> ShopRankProbabilityByLevel = new Dictionary<int, Dictionary<CardRank, int>>
        {
            { 1, new Dictionary<CardRank, int> { { CardRank.Bronze, 100 } } },
            { 2, new Dictionary<CardRank, int> { { CardRank.Bronze, 100 } } },
            { 3, new Dictionary<CardRank, int> { { CardRank.Bronze, 100 } } },
            { 4, new Dictionary<CardRank, int> { { CardRank.Bronze, 75 }, { CardRank.Silver, 25 } } },
            { 5, new Dictionary<CardRank, int> { 
                { CardRank.Bronze, 55 }, 
                { CardRank.Silver, 30 }, 
                { CardRank.Gold, 15 }, 
                { CardRank.Diamond, 0 } 
            } }
        };

        private static CardRank RollCardRank(int playerLevel, Random random)
        {
            if (!ShopRankProbabilityByLevel.TryGetValue(playerLevel, out var probabilities))
            {
                return CardRank.Bronze;
            }

            int totalWeight = probabilities.Values.Sum();
            int roll = random.Next(totalWeight);

            int cumulative = 0;
            foreach (var kvp in probabilities)
            {
                cumulative += kvp.Value;
                if (roll < cumulative)
                {
                    return kvp.Key;
                }
            }

            return CardRank.Bronze;
        }

        private static CardRank GetCardRank(string rank)
        {
            return rank switch
            {
                "Bronze" => CardRank.Bronze,
                "Silver" => CardRank.Silver,
                "Gold" => CardRank.Gold,
                "Diamond" => CardRank.Diamond,
                _ => CardRank.Bronze
            };
        }
    }
}
