using Framework;

namespace Game
{
    /// <summary>
    /// 钻石变更成功事件
    /// </summary>
    public struct DiamondChangedEvent : IEvent
    {
        /// <summary>变更数量（正数为增加，负数为减少）</summary>
        public long Amount;
        /// <summary>当前数量</summary>
        public long Current;
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
        public long Amount;
        /// <summary>当前数量</summary>
        public long Current;
    }

    /// <summary>
    /// 金币变更失败事件
    /// </summary>
    public struct GoldChangeFailedEvent : IEvent
    {
        /// <summary>失败原因</summary>
        public string Reason;
    }
}
