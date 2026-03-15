using Framework;
using System.Collections.Generic;

namespace Game.Gameplay.CardBattle
{
    public interface ICardEffect
    {
        void Execute(CardBattleContext context);
    }

    /// <summary>
    /// 卡牌执行上下文：卡牌打出或生效时携带的环境变量
    /// </summary>
    public class CardBattleContext
    {
        public EntityData Source { get; set; }
        public List<EntityData> Targets { get; set; }
        public CardData Card { get; set; }
    }

    public enum CardTargetType
    {
        None,
        Self,
        SingleEnemy,
        AllEnemies,
        RandomEnemy
    }

    /// <summary>
    /// 运行时的卡牌实例模型
    /// </summary>
    public class CardData
    {
        // 唯一实例ID（同一张牌可能有多张拷贝）
        public string InstanceId { get; set; }
        
        // 静态配置表ID
        public int ConfigId { get; set; }
        public string Name { get; set; }
        public BindableProperty<string> Description { get; } = new BindableProperty<string>("");

        public BindableProperty<int> BaseCost { get; } = new BindableProperty<int>(1);
        public BindableProperty<int> CurrentCost { get; } = new BindableProperty<int>(1);

        public CardTargetType TargetType { get; set; }

        // 打出该牌时，往队列压入的作用器集
        public List<ICardEffect> OnPlayEffects { get; } = new List<ICardEffect>();
        
        // （在此还能拓展 OnDrawnEffects, OnRetainedEffects 等）
    }
}

