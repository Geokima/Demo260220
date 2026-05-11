using System;
using System.Collections.Generic;
using Framework;
using UnityEngine;

namespace Game.Gameplay.Demo1
{
    public class CardModel : AbstractModel
    {
        private Demo1CardConfig _config;

        public int Id => _config?.Id ?? 0;
        public string Name => _config?.Name;
        public int Size => _config?.Size ?? 1;
        public List<string> Tags => _config?.Tags ?? new List<string>();
        public CardType Type => _config != null && Enum.TryParse<CardType>(_config.Type, out var t) ? t : CardType.Active;

        public BindableProperty<int> Price { get; } = new BindableProperty<int>();
        public BindableProperty<int> Damage { get; } = new BindableProperty<int>();
        public BindableProperty<int> Shield { get; } = new BindableProperty<int>();
        public BindableProperty<int> Poison { get; } = new BindableProperty<int>();
        public BindableProperty<int> Cure { get; } = new BindableProperty<int>();
        public BindableProperty<int> BulletCount { get; } = new BindableProperty<int>();

        public BindableProperty<float> MaxCD { get; } = new BindableProperty<float>();
        public BindableProperty<float> CurrentCD { get; } = new BindableProperty<float>();

        public BindableProperty<CardRank> Rank { get; } = new BindableProperty<CardRank>();
        public BindableProperty<int> StartIndex { get; } = new BindableProperty<int>(0);

        public Demo1CardConfig Config => _config;

        public CardModel()
        {
        }

        public CardModel(Demo1CardConfig config)
        {
            Bind(config);
        }

        public void Bind(Demo1CardConfig config)
        {
            _config = config;
            if (config == null) return;

            SyncFromConfig();
        }

        public void SyncFromConfig()
        {
            if (_config == null) return;

            Price.Value = _config.Price;
            Damage.Value = _config.Damage;
            Shield.Value = _config.Shield;
            Poison.Value = _config.Poison;
            Cure.Value = _config.Cure;
            BulletCount.Value = _config.BulletCount;
            MaxCD.Value = _config.MaxCD;
            CurrentCD.Value = _config.MaxCD;
            Rank.Value = _config.Rank != null && Enum.TryParse<CardRank>(_config.Rank, out var r) ? r : CardRank.Bronze;
        }

        public void ApplyPriceRule(bool isInShop)
        {
            if (_config == null)
                return;

            if (isInShop)
            {
                Price.Value = _config.Price;
                return;
            }

            Price.Value = Mathf.FloorToInt(_config.Price * 0.5f);
        }

        public void ResetCD()
        {
            CurrentCD.Value = MaxCD.Value;
        }

        public override void Init()
        {
        }
    }
}
