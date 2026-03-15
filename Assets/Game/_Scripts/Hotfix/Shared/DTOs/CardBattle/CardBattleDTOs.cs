using System.Collections.Generic;

namespace Game.Shared.DTOs.CardBattle
{
    /// <summary>
    /// 战? Buff 数据传输对象
    /// </summary>
    public class BuffDto
    {
        public string Id { get; set; }
        public int Value { get; set; }
        public int TurnDuration { get; set; } // -1 表示永久
    }

    /// <summary>
    /// 完整的战斗同步快照，用于后端校验和断线重?
    /// </summary>
    public class BattleSyncData
    {
        public int RoundCount { get; set; }
        public EntityDto Player { get; set; }
        public List<EntityDto> Monsters { get; set; }
        public List<int> CardHands { get; set; }
        public List<int> CardDrawPile { get; set; }
        public List<int> CardDiscardPile { get; set; }
    }

    public class EntityDto
    {
        public int InstanceId { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public int Block { get; set; }
        public List<BuffDto> Buffs { get; set; }
    }
}
