namespace Game.Consts
{
    public enum ErrorCode
    {
        Success = 0,

        InvalidParams = 4,
        Failed = 5,
        
        UserNotFound = 100,

        InventoryFull = 200,
        ItemNotFound = 201,
        InventoryEmpty = 203,
        ItemCountInsufficient = 204,

        InsufficientGold = 210,
        InsufficientDiamond = 211,

        MailNotFound = 500,
        MailAttachmentReceived = 501,
        MailNoAttachment = 502,

        ShopItemNotFound = 600,
        ShopLimitReached = 601,
        ShopRefreshLimitReached = 602
    }
}
