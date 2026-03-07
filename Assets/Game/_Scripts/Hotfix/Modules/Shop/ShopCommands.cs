using Framework;

namespace Game.Shop
{
    public class GetShopListCommand : AbstractCommand
    {
        public string ShopType { get; set; }

        public override void Execute(object sender)
        {
            this.GetSystem<ShopService>().RequestGetShopList(ShopType);
        }
    }

    public class RefreshShopCommand : AbstractCommand
    {
        public string ShopType { get; set; }

        public override void Execute(object sender)
        {
            this.GetSystem<ShopService>().RequestRefreshShop(ShopType);
        }
    }

    public class BuyShopItemCommand : AbstractCommand
    {
        public int ShopItemId { get; set; }
        public int Count { get; set; }

        public override void Execute(object sender)
        {
            this.GetSystem<ShopService>().RequestBuyItem(ShopItemId, Count);
        }
    }
}
