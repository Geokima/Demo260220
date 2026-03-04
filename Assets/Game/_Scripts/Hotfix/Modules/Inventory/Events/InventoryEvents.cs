using Framework;
using Game.DTOs;

namespace Game.Inventory
{
    /// <summary>
    /// 背包数据更新事件
    /// </summary>
    public struct InventoryUpdatedEvent : IEvent
    {
        public InventoryDTO Inventory;
    }

    /// <summary>
    /// 物品添加成功事件
    /// </summary>
    public struct ItemAddedEvent : IEvent
    {
        public int ItemId;
        public int Amount;
    }

    /// <summary>
    /// 物品移除成功事件
    /// </summary>
    public struct ItemRemovedEvent : IEvent
    {
        public string Uid;
        public int Amount;
    }

    /// <summary>
    /// 物品使用成功事件
    /// </summary>
    public struct ItemUsedEvent : IEvent
    {
        public string Uid;
        public int Amount;
        public ItemEffect Effect;
    }

    /// <summary>
    /// 物品操作失败事件
    /// </summary>
    public struct ItemOperationFailedEvent : IEvent
    {
        public string Reason;
    }
}
