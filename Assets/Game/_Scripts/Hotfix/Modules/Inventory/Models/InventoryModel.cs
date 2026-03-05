using System.Collections.Generic;
using Framework;
using Game.DTOs;

namespace Game.Inventory
{
    public class InventoryModel : AbstractModel
    {
        #region Fields

        // 使用嵌套响应式：字典管格子存亡，BindableProperty 管数值变动
        public BindableDictionary<string, BindableProperty<ItemData>> Slots = new();
        public BindableProperty<int> MaxSlots = new BindableProperty<int>(int.MaxValue);
        private Dictionary<int, List<string>> _itemIdToUids = new();

        #endregion

        #region Public Methods

        public BindableProperty<ItemData> GetSlot(string uid)
        {
            return Slots.TryGetValue(uid, out var prop) ? prop : null;
        }

        public List<string> GetUidsByItemId(int itemId)
        {
            return _itemIdToUids.GetValueOrDefault(itemId) ?? new List<string>();
        }

        public List<ItemData> GetAllItems()
        {
            var results = new List<ItemData>();
            foreach (var prop in Slots.Values)
            {
                results.Add(prop.Value);
            }
            return results;
        }

        public List<ItemData> GetFilteredItemsByItemId(int itemId)
        {
            var uids = GetUidsByItemId(itemId);
            var results = new List<ItemData>();
            foreach (var uid in uids)
            {
                if (Slots.TryGetValue(uid, out var prop))
                    results.Add(prop.Value);
            }
            return results;
        }

        public int GetItemCount(int itemId)
        {
            int total = 0;
            var uids = GetUidsByItemId(itemId);
            foreach (var uid in uids)
            {
                if (Slots.TryGetValue(uid, out var prop))
                    total += prop.Value.Count;
            }
            return total;
        }

        public bool HasEnough(int itemId, int amount)
        {
            return GetItemCount(itemId) >= amount;
        }

        #endregion

        #region Sync

        public void SyncAll(InventoryData data)
        {
            Slots.Clear();
            if (data.Items != null)
            {
                foreach (var item in data.Items)
                {
                    Slots[item.Uid] = new BindableProperty<ItemData>(item);
                }
            }
            RebuildItemIdIndex();
            MaxSlots.Value = data.MaxSlots;
        }

        public void SyncDiff(InventorySyncData data)
        {
            // 1. 处理移除
            if (data.RemovedUids != null)
            {
                foreach (var uid in data.RemovedUids)
                {
                    Slots.Remove(uid); // 触发 Slots.OnRemove
                }
            }

            // 2. 处理变动（新增或修改）
            if (data.ChangedItems != null)
            {
                foreach (var item in data.ChangedItems)
                {
                    if (Slots.TryGetValue(item.Uid, out var prop))
                    {
                        prop.Value = item; // 已有格子：触发属性变动事件（二级响应）
                    }
                    else
                    {
                        Slots[item.Uid] = new BindableProperty<ItemData>(item); // 新增格子：触发字典 Add 事件（一级响应）
                    }
                }
            }

            // 同步后重建索引（也可以在具体 Add/Remove 里做局部维护以优化性能）
            RebuildItemIdIndex();

            if (data.NewSlots > 0)
            {
                MaxSlots.Value = data.NewSlots;
            }
        }

        #endregion

        #region Private Methods

        private void RebuildItemIdIndex()
        {
            _itemIdToUids.Clear();
            foreach (var kvp in Slots)
            {
                int itemId = kvp.Value.Value.ItemId;
                if (!_itemIdToUids.ContainsKey(itemId))
                    _itemIdToUids[itemId] = new List<string>();
                _itemIdToUids[itemId].Add(kvp.Key);
            }
        }

        #endregion

        #region Lifecycle

        public override void Deinit()
        {
            Clear();
        }

        public void Clear()
        {
            Slots.Clear();
            _itemIdToUids.Clear();
            MaxSlots.Value = 9;
        }

        #endregion
    }
}
