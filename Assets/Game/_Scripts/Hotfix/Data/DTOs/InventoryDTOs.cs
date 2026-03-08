using System;
using System.Collections.Generic;
using Game.Base;
using Newtonsoft.Json;
using UnityEngine.Serialization;

namespace Game.DTOs
{
    #region Item

    [Serializable]
    public class ObtainItem
    {
        [JsonProperty("type")]
        public string Type; // item / currency / product??
        [JsonProperty("itemId")]
        public int ItemId;      // 101, 2001...
        [JsonProperty("count")]
        public int Count;

        [JsonProperty("expireTime")]
        public long ExpireTime;
    }
    
    [Serializable]
    public class ItemData
    {
        [JsonProperty("uid")]
        public string Uid;

        [JsonProperty("itemId")]
        public int ItemId;

        [JsonProperty("count")]
        public int Count;

        [JsonProperty("expireTime")]
        public long ExpireTime;
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
        MISSION = 5,    // 任务奖励
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
    public class UseItemRequest : BaseRequest
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
    public class AddItemRequest : BaseRequest
    {
        [JsonProperty("itemId")]
        public int ItemId;

        [JsonProperty("amount")]
        public int Amount;
    }

    #endregion

    #region RemoveItem

    [Serializable]
    public class RemoveItemRequest : BaseRequest
    {
        [JsonProperty("uid")]
        public string Uid;

        [JsonProperty("amount")]
        public int Amount;
    }

    #endregion
}
