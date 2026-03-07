using System.Collections.Generic;
using Framework;
using Game.DTOs;

namespace Game.Shop
{
    public class ShopModel : AbstractModel
    {
        public BindableProperty<long> RefreshTime = new(0);
        public BindableDictionary<int, BindableProperty<ShopItemData>> Items = new();

        public void Sync(ShopListData data)
        {
            if (data == null) return;

            RefreshTime.Value = data.RefreshTime;
            Items.Clear();

            if (data.Items != null)
            {
                foreach (var item in data.Items)
                {
                    Items[item.ShopItemId] = new BindableProperty<ShopItemData>(item);
                }
            }
        }

        public bool CanBuy(int shopItemId)
        {
            if (Items.TryGetValue(shopItemId, out var prop))
                return prop.Value?.CanBuy ?? false;
            return false;
        }

        public override void Deinit()
        {
            Clear();
        }

        public void Clear()
        {
            RefreshTime.Value = 0;
            Items.Clear();
        }
    }
}
