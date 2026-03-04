using System;
using Newtonsoft.Json;

namespace Game.DTOs
{
    /// <summary>
    /// 物品数据传输对象
    /// </summary>
    [Serializable]
    public class ItemDTO
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
    /// 物品使用效果数据传输对象
    /// </summary>
    [Serializable]
    public struct ItemEffect
    {
        public string Type;
        public int ItemId;
    }

    /// <summary>
    /// 背包数据传输对象
    /// </summary>
    [Serializable]
    public class InventoryDTO
    {
        /// <summary>物品列表</summary>
        public ItemDTO[] items;
        /// <summary>最大格子数</summary>
        public int maxSlots;
    }

    /// <summary>
    /// 使用物品请求数据
    /// </summary>
    public class UseItemData
    {
        [JsonProperty("inventory")]
        public InventoryDTO Inventory { get; set; }

        [JsonProperty("effect")]
        public ItemEffect Effect { get; set; }
    }

    /// <summary>
    /// 使用物品响应
    /// </summary>
    public class UseItemResponse : BaseResponse<UseItemData> { }
}
