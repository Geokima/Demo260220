using System.Collections.Generic;
using Framework;
using Game.DTOs;

namespace Game.Inventory
{
    /// <summary>
    /// 背包增量同步事件 - 全系统唯一的数据真相对齐入口
    /// </summary>
    public struct InventorySyncEvent : IEvent
    {
        public InventorySyncData SyncData;
    }

    /// <summary>
    /// 物品使用成功表现事件 - 用于 UI 播放特定的动作/特效
    /// </summary>
    public struct ItemUsedEvent : IEvent
    {
        public string Uid;
        public int Amount;
        public List<ItemEffect> Effects;
    }

    /// <summary>
    /// 物品操作失败事件
    /// </summary>
    public struct ItemOperationFailedEvent : IEvent
    {
        public string Reason;
    }
}
