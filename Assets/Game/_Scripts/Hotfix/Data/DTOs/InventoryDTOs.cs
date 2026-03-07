using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.DTOs
{
    #region Item

    [Serializable]
    public class ItemData
    {
        [JsonProperty("uid")]
        public string Uid;

        [JsonProperty("itemId")]
        public int ItemId;

        [JsonProperty("count")]
        public int Count;
    }

    [Serializable]
    public class ItemEffect
    {
        [JsonProperty("effectId")]
        public int EffectId;

        [JsonProperty("params")]
        public Dictionary<string, string> Params;
    }

    #endregion

    #region Inventory

    public enum InventorySyncReason
    {
        UNKNOWN = 0,
        LOGIN = 1,      // 登录全量同步
        DROP = 2,       // 掉落获得
        USE = 3,        // 使用消耗
        BUY = 4,        // 购买获得
        TASK = 5,       // 任务奖励
        MAIL = 6        // 邮件奖励
    }

    [Serializable]
    public class InventoryData
    {
        [JsonProperty("items")]
        public ItemData[] Items;

        [JsonProperty("maxSlots")]
        public int MaxSlots;

        [JsonProperty("revision")]
        public long Revision;
    }

    [Serializable]
    public class InventorySyncData
    {
        [JsonProperty("changedItems")]
        public List<ItemData> ChangedItems;

        [JsonProperty("removedUids")]
        public List<string> RemovedUids;

        [JsonProperty("newSlots")]
        public int NewSlots;

        [JsonProperty("reason")]
        public InventorySyncReason Reason;

        [JsonProperty("revision")]
        public long Revision;
    }

    public class GetInventoryResponse : BaseResponse<InventoryData> { }
    public class InventoryResponse : BaseResponse<InventorySyncData> { }

    #endregion

    #region UseItem

    [Serializable]
    public class UseItemRequest
    {
        [JsonProperty("uid")]
        public string Uid;

        [JsonProperty("amount")]
        public int Amount;
        
        [JsonProperty("params")]
        public Dictionary<string, string> Params = new(); 
    }

    public class UseItemResponse : BaseResponse<List<ItemEffect>> { }

    #endregion

    #region AddItem

    [Serializable]
    public class AddItemRequest
    {
        [JsonProperty("itemId")]
        public int ItemId;

        [JsonProperty("amount")]
        public int Amount;
    }

    #endregion

    #region RemoveItem

    [Serializable]
    public class RemoveItemRequest
    {
        [JsonProperty("uid")]
        public string Uid;

        [JsonProperty("amount")]
        public int Amount;
    }

    #endregion
}
