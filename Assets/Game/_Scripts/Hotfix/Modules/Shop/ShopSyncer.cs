using Framework;
using Game.Base;
using Game.DTOs;

namespace Game.Shop
{
    public class ShopSyncer : BaseSyncer
    {
        public void SyncShopResponse(ShopListResponse response)
        {
            if (response == null || response.Code != 0 || response.Data == null) return;
            
            this.GetModel<ShopModel>().Sync(response.Data);
            this.SendEvent(new ShopListUpdatedEvent 
            { 
                Data = response.Data, 
                ShopType = response.Data.ShopType 
            });
        }

        public void SyncShopResponse(ShopRefreshResponse response)
        {
            if (response == null || response.Code != 0 || response.Data == null) return;
            
            this.GetModel<ShopModel>().Sync(response.Data);
            this.SendEvent(new ShopListUpdatedEvent 
            { 
                Data = response.Data, 
                ShopType = response.Data.ShopType 
            });
        }

        public void SyncShopResponse(ShopBuyResponse response)
        {
            if (response == null || response.Code != 0 || response.Data?.ShopSync == null) return;
            
            this.GetModel<ShopModel>().Sync(response.Data.ShopSync);
            this.SendEvent(new ShopListUpdatedEvent 
            { 
                Data = response.Data.ShopSync, 
                ShopType = response.Data.ShopSync.ShopType 
            });
        }
    }
}
