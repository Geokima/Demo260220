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
        public BindableProperty<long> Revision = new BindableProperty<long>(0);
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

        public bool SyncAll(InventoryData data)
        {
            if (data == null) return false;
            if (data.Revision > 0 && data.Revision <= Revision.Value) return false;

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
            Revision.Value = data.Revision;
            return true;
        }

        // 2. 修改后的 SyncDiff，彻底去掉全表遍历
        public bool SyncDiff(InventorySyncData data)
        {
            if (data == null) return false;
            if (data.Revision > 0 && data.Revision <= Revision.Value) return false;

            // 1. 处理移除：同步维护索引
            if (data.RemovedUids != null)
            {
                foreach (var uid in data.RemovedUids)
                {
                    if (Slots.TryGetValue(uid, out var prop))
                    {
                        UpdateItemIndex(uid, prop.Value.ItemId, true); // 增量移除索引
                        Slots.Remove(uid); 
                    }
                }
            }

            // 2. 处理变动：同步维护索引
            if (data.ChangedItems != null)
            {
                foreach (var item in data.ChangedItems)
                {
                    if (Slots.TryGetValue(item.Uid, out var prop))
                    {
                        // 如果 ItemId 变了（虽然极少发生），更新索引映射
                        if (prop.Value.ItemId != item.ItemId)
                        {
                            UpdateItemIndex(item.Uid, prop.Value.ItemId, true);
                            UpdateItemIndex(item.Uid, item.ItemId, false);
                        }
                        prop.Value = item; 
                    }
                    else
                    {
                        Slots[item.Uid] = new BindableProperty<ItemData>(item);
                        UpdateItemIndex(item.Uid, item.ItemId, false); // 增量新增索引
                    }
                }
            }

            if (data.NewSlots > 0) MaxSlots.Value = data.NewSlots;
            Revision.Value = data.Revision;
            return true;
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
        
        private void UpdateItemIndex(string uid, int itemId, bool isRemove)
        {
            if (isRemove)
            {
                if (_itemIdToUids.TryGetValue(itemId, out var uids))
                {
                    uids.Remove(uid);
                    if (uids.Count == 0) _itemIdToUids.Remove(itemId);
                }
            }
            else
            {
                if (!_itemIdToUids.ContainsKey(itemId))
                    _itemIdToUids[itemId] = new List<string>();
        
                if (!_itemIdToUids[itemId].Contains(uid))
                    _itemIdToUids[itemId].Add(uid);
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
        }

        #endregion
    }
}
