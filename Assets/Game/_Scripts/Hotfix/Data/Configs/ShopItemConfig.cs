using Framework.Modules.Config;

namespace Game.Config
{
    public class ShopItemConfig : IConfigRow
    {
        public int Id { get; set; }
        public string ShopType { get; set; }
        public int ItemId { get; set; }
        public int ItemCount { get; set; }
        public string PriceType { get; set; }
        public int OriginalPrice { get; set; }
        public int Price { get; set; }
        public float Discount { get; set; }
        public int Weight { get; set; }
        public int LimitCount { get; set; }
        public long StartTime { get; set; }
        public long EndTime { get; set; }
    }
}
