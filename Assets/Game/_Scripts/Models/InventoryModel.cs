using Framework;
using Game.Data;
using System.Collections.Generic;
using System.Linq;

namespace Game.Models
{
    /// <summary>
    /// 背包数据模型
    /// </summary>
    public class InventoryModel : AbstractModel
    {
        /// <summary>物品列表</summary>
        public BindableList<ItemData> Items = new BindableList<ItemData>();
        /// <summary>最大格子数</summary>
        public BindableProperty<int> MaxSlots = new BindableProperty<int>(9);

        /// <summary>
        /// 设置背包数据
        /// </summary>
        public void SetInventory(InventoryData data)
        {
            Items.Clear();
            if (data.items != null)
            {
                foreach (var item in data.items)
                    Items.Add(item);
            }
            MaxSlots.Value = data.maxSlots;
        }

        /// <summary>
        /// 获取指定UID的物品
        /// </summary>
        public ItemData GetItem(string uid)
        {
            return Items.FirstOrDefault(item => item.uid == uid);
        }

        /// <summary>
        /// 获取指定物品ID的总数量
        /// </summary>
        public int GetItemCount(int itemId)
        {
            return Items.Where(item => item.itemId == itemId).Sum(item => item.count);
        }

        /// <summary>
        /// 检查物品是否足够
        /// </summary>
        public bool HasEnough(int itemId, int amount)
        {
            return GetItemCount(itemId) >= amount;
        }

        public override void Deinit()
        {
            Items.Clear();
            MaxSlots.Value = 9;
        }

        /// <summary>
        /// 清除所有数据（退出登录时调用）
        /// </summary>
        public void Clear()
        {
            Items.Clear();
            MaxSlots.Value = 9;
        }
    }
}
