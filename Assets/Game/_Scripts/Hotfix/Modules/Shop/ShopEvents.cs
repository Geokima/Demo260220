using Game.DTOs;

namespace Game.Shop
{
    public struct ShopListUpdatedEvent
    {
        public ShopListData Data { get; set; }
        public string ShopType { get; set; }
    }

    public struct ShopPurchaseSuccessEvent
    {
        public string Message { get; set; }
    }

    public struct ShopPurchaseFailedEvent
    {
        public string Message { get; set; }
    }
}
