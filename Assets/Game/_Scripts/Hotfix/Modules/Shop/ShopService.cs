using Framework;
using Game.Base;
using Game.DTOs;
using Game.Gateways;

namespace Game.Shop
{
    public class ShopService : BaseService
    {
        private IServerGateway _gateway;

        public override void Init()
        {
            _gateway = this.GetSystem<IServerGateway>();
        }

        public async void RequestGetShopList(string shopType)
        {
            var resp = await _gateway.PostAsync<ShopListRequest, ShopListResponse>("/shop/list", new ShopListRequest { ShopType = shopType });
            if (resp.Code == 0)
            {
                this.GetSyncer<ShopSyncer>().SyncShopResponse(resp);
            }
        }

        public async void RequestRefreshShop(string shopType)
        {
            var resp = await _gateway.PostAsync<ShopRefreshRequest, ShopRefreshResponse>("/shop/refresh", new ShopRefreshRequest { ShopType = shopType });
            if (resp.Code == 0)
            {
                this.GetSyncer<ShopSyncer>().SyncShopResponse(resp);
            }
        }

        public async void RequestBuyItem(int shopItemId, int count)
        {
            var resp = await _gateway.PostAsync<ShopBuyRequest, ShopBuyResponse>("/shop/buy", new ShopBuyRequest { ShopItemId = shopItemId, Count = count });
            if (resp.Code == 0)
            {
                this.GetSyncer<ShopSyncer>().SyncShopResponse(resp);
            }
        }
    }
}
