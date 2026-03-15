using Framework;
using System.Collections.Generic;

namespace Game.Gameplay.CardBattle
{
    /// <summary> 对局核心数据模型 </summary>
    public class BattleModel : AbstractModel
    {
        public BindableProperty<int> TurnCount { get; } = new BindableProperty<int>(0);
        
        /// <summary> 视觉锁计数。非 0 时表示表现层忙碌（如动画），逻辑层需等待其归 0。 </summary>
        public BindableProperty<int> VisualLockCount { get; } = new BindableProperty<int>(0);

        // 牌堆管理 - 使用 BindableList 以便 UI 监听
        public BindableList<CardData> DrawPile { get; } = new BindableList<CardData>();
        public BindableList<CardData> Hand { get; } = new BindableList<CardData>();
        public BindableList<CardData> DiscardPile { get; } = new BindableList<CardData>();
        public BindableList<CardData> ExhaustPile { get; } = new BindableList<CardData>(); 

        // 参战实体
        public EntityData Player { get; set; }
        public List<EntityData> Enemies { get; } = new List<EntityData>();

        // 交互状态 (交互层与逻辑层的纽带)
        public BindableProperty<CardData> SelectedCard { get; } = new BindableProperty<CardData>(null);
        public BindableProperty<EntityData> SelectedEnemy { get; } = new BindableProperty<EntityData>(null);

        public override void Init()
        {
            TurnCount.Value = 0;
            VisualLockCount.Value = 0;
            SelectedCard.Value = null;
            SelectedEnemy.Value = null;
            DrawPile.Clear();
            Hand.Clear();
            DiscardPile.Clear();
            ExhaustPile.Clear();
            Enemies.Clear();
        }
    }
}
