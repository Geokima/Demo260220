using Framework;

namespace Game
{
    /// <summary>
    /// 钻石变更成功事件
    /// </summary>
    public struct DiamondChangedEvent : IEvent
    {
        /// <summary>变更数量（正数为增加，负数为减少）</summary>
        public int Amount;
        /// <summary>当前数量</summary>
        public int Current;
    }

    /// <summary>
    /// 钻石变更失败事件
    /// </summary>
    public struct DiamondChangeFailedEvent : IEvent
    {
        /// <summary>失败原因</summary>
        public string Reason;
    }

    /// <summary>
    /// 金币变更成功事件
    /// </summary>
    public struct GoldChangedEvent : IEvent
    {
        /// <summary>变更数量（正数为增加，负数为减少）</summary>
        public int Amount;
        /// <summary>当前数量</summary>
        public int Current;
    }

    /// <summary>
    /// 金币变更失败事件
    /// </summary>
    public struct GoldChangeFailedEvent : IEvent
    {
        /// <summary>失败原因</summary>
        public string Reason;
    }

    /// <summary>
    /// 经验变更成功事件
    /// </summary>
    public struct ExpChangedEvent : IEvent
    {
        /// <summary>变更数量</summary>
        public int Amount;
        /// <summary>当前经验</summary>
        public int Current;
        /// <summary>当前等级</summary>
        public int Level;
    }

    /// <summary>
    /// 经验变更失败事件
    /// </summary>
    public struct ExpChangeFailedEvent : IEvent
    {
        /// <summary>失败原因</summary>
        public string Reason;
    }

    /// <summary>
    /// 升级事件
    /// </summary>
    public struct LevelUpEvent : IEvent
    {
        /// <summary>旧等级</summary>
        public int OldLevel;
        /// <summary>新等级</summary>
        public int NewLevel;
    }

    /// <summary>
    /// 体力变更成功事件
    /// </summary>
    public struct EnergyChangedEvent : IEvent
    {
        /// <summary>变更数量</summary>
        public int Amount;
        /// <summary>当前体力</summary>
        public int Current;
        /// <summary>最大体力</summary>
        public int Max;
    }

    /// <summary>
    /// 体力变更失败事件
    /// </summary>
    public struct EnergyChangeFailedEvent : IEvent
    {
        /// <summary>失败原因</summary>
        public string Reason;
    }
}
