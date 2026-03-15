using System.Collections.Generic;
using Framework;

namespace Game.Gameplay.CardBattle
{
    public struct BattleStartEvent { }
    public struct BattleEndEvent { public bool IsWin; }
    public struct TurnStartEvent { public EntityType CurrentTurn; public int TurnCount; }
    public struct TurnEndEvent { public EntityType CurrentTurn; }

    /// <summary>
    /// 视觉表现事件基类
    /// </summary>
    public abstract class VisualEffectEvent
    {
        public object Sender { get; set; }
    }

    public class DamageVisualEvent : VisualEffectEvent
    {
        public EntityData Source { get; set; }
        public EntityData Target { get; set; }
        public int Amount { get; set; }
    }

    public class BlockVisualEvent : VisualEffectEvent
    {
        public EntityData Target { get; set; }
        public int Amount { get; set; }
    }

    public class BuffVisualEvent : VisualEffectEvent
    {
        public EntityData Target { get; set; }
        public string BuffId { get; set; }
        public int Value { get; set; }
    }

    public struct CardDrawnEvent
    {
        public CardData Card;
    }
}