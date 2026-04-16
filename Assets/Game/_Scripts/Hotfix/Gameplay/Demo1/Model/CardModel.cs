using System.Collections.Generic;
using Framework;

namespace Game.Gameplay.Demo1
{
    /// <summary>
    /// 卡牌数据（运行时）
    /// 包含战斗中的动态数值，使用 BindableProperty 响应式更新 UI
    /// </summary>
    public class CardModel : AbstractModel
    {
        private CardData _config;

        public string Id => _config?.Id;
        public string Name => _config?.Name;
        public int Size => _config?.Size ?? 1;
        public List<string> Tags => _config?.Tags ?? new List<string>();
        public CardType Type => _config?.Type ?? CardType.Active;

        public BindableProperty<int> Price { get; } = new BindableProperty<int>();
        public BindableProperty<int> Damage { get; } = new BindableProperty<int>();
        public BindableProperty<int> Shield { get; } = new BindableProperty<int>();
        public BindableProperty<int> Poison { get; } = new BindableProperty<int>();
        public BindableProperty<int> Cure { get; } = new BindableProperty<int>();
        public BindableProperty<int> BulletCount { get; } = new BindableProperty<int>();

        public BindableProperty<float> MaxCD { get; } = new BindableProperty<float>();
        public BindableProperty<float> CurrentCD { get; } = new BindableProperty<float>();

        public BindableProperty<CardRank> Rank { get; } = new BindableProperty<CardRank>();

        public CardData Config => _config;

        public CardModel()
        {
        }

        public CardModel(CardData config)
        {
            Bind(config);
        }

        public void Bind(CardData config)
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
            Rank.Value = _config.Rank;
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
