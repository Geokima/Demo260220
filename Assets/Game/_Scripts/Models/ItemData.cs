using System;

namespace Game.Models
{
    /// <summary>
    /// 物品数据
    /// </summary>
    [Serializable]
    public class ItemData
    {
        /// <summary>物品唯一ID</summary>
        public string uid;
        /// <summary>物品配置ID</summary>
        public int itemId;
        /// <summary>数量</summary>
        public int count;
        /// <summary>是否绑定</summary>
        public bool bind;
    }

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

    /// <summary>
    /// 物品配置
    /// </summary>
    [Serializable]
    public class ItemConfig
    {
        /// <summary>物品名称</summary>
        public string name;
        /// <summary>物品类型</summary>
        public string type;
        /// <summary>最大堆叠数</summary>
        public int maxStack;
    }
}
