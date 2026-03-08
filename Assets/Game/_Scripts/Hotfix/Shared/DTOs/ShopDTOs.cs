using System;
using System.Collections.Generic;
using Game.Base;
using Newtonsoft.Json;

namespace Game.DTOs
{
    #region ShopList

    [Serializable]
    public class ShopListRequest
    {
        [JsonProperty("shopType")]
        public string ShopType { get; set; }
    }

    [Serializable]
    public class ShopItemData
    {
        [JsonProperty("shopItemId")]
        public int ShopItemId { get; set; }

        [JsonProperty("itemId")]
        public int ItemId { get; set; }

        [JsonProperty("itemCount")]
        public int ItemCount { get; set; }

        [JsonProperty("priceType")]
        public string PriceType { get; set; }

        [JsonProperty("originalPrice")]
        public int OriginalPrice { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("discount")]
        public float Discount { get; set; }

        [JsonProperty("limitCount")]
        public int LimitCount { get; set; }

        [JsonProperty("purchasedCount")]
        public int PurchasedCount { get; set; }

        [JsonProperty("canBuy")]
        public bool CanBuy { get; set; }
    }

    [Serializable]
    public class ShopListData
    {
        [JsonProperty("shopType")]
        public string ShopType { get; set; }

        [JsonProperty("items")]
        public List<ShopItemData> Items { get; set; }

        [JsonProperty("refreshCount")]
        public int RefreshCount { get; set; }

        [JsonProperty("maxRefreshCount")]
        public int MaxRefreshCount { get; set; }

        [JsonProperty("canRefresh")]
        public bool CanRefresh { get; set; }

        [JsonProperty("refreshTime")]
        public long RefreshTime { get; set; }
    }

    public class ShopListResponse : BaseResponse<ShopListData> { }

    #endregion

    #region ShopBuy

    [Serializable]
    public class ShopBuyRequest
    {
        [JsonProperty("shopItemId")]
        public int ShopItemId { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }
    }

    [Serializable]
    public class ShopBuyResponseData
    {
        [JsonProperty("inventorySync")]
        public InventorySyncData InventorySync { get; set; }

        [JsonProperty("playerSync")]
        public PlayerData PlayerSync { get; set; }

        [JsonProperty("shopSync")]
        public ShopListData ShopSync { get; set; }
    }

    public class ShopBuyResponse : BaseResponse<ShopBuyResponseData> { }

    #endregion

    #region ShopRefresh

    [Serializable]
    public class ShopRefreshRequest
    {
        [JsonProperty("shopType")]
        public string ShopType { get; set; }
    }

    public class ShopRefreshResponse : BaseResponse<ShopListData> { }

    #endregion
}
