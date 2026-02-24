using System;

namespace Game.Data
{
    /// <summary>
    /// 背包数据
    /// </summary>
    [Serializable]
    public class InventoryData
    {
        /// <summary>物品列表</summary>
        public ItemData[] items;
        /// <summary>最大格子数</summary>
        public int maxSlots;
    }
}
