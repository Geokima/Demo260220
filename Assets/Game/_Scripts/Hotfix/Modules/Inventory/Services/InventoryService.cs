using Cysharp.Threading.Tasks;
using Framework;
using Game.Auth;
using Game.Base;
using Game.DTOs;
using UnityEngine;

namespace Game.Inventory
{
    /// <summary>
    /// 背包服务 - 处理物品相关业务流程（业务导演）
    /// </summary>
    public class InventoryService : BaseService
    {
        public override void Init()
        {
            this.RegisterEvent<LoginSuccessEvent>(OnLoginSuccess);
        }

        private async void OnLoginSuccess(LoginSuccessEvent e)
        {
            var response = await GetInventoryInternalAsync();
            if (response != null && response.Code == 0)
            {
                this.GetSyncer<InventorySyncer>().SyncInventoryResponse(response);
            }
        }

        public async UniTask<InventoryResponse> GetInventoryAsync()
        {
            return await GetInventoryInternalAsync();
        }

        public void RequestGetInventory()
        {
            GetInventoryAsync().Forget();
        }

        private async UniTask<InventoryResponse> GetInventoryInternalAsync()
        {
            Debug.Log("[InventoryService] 请求获取全量背包...");

            var response = await NetworkClient.PostAsync<InventoryResponse>("/inventory/get");

            if (response != null && response.Code == 0)
            {
                this.GetSyncer<InventorySyncer>().SyncInventoryResponse(response);
            }
            else
            {
                string error = response?.Msg ?? "获取背包失败";
                Debug.LogError($"[InventoryService] Get inventory error: {error}");
                this.SendEvent(new ItemOperationFailedEvent { Reason = error });
            }
            return response;
        }

        public void RequestAddItem(int itemId, int amount, bool bind = false)
        {
            AddItemAsync(itemId, amount, bind).Forget();
        }

        private async UniTaskVoid AddItemAsync(int itemId, int amount, bool bind)
        {
            Debug.Log($"[InventoryService] 申请添加物品: {itemId}, 数量: {amount}");

            var response = await NetworkClient.PostAsync<object, InventoryResponse>("/inventory/add",
                new { itemId = itemId, amount = amount, bind = bind });

            if (response != null && response.Code == 0)
            {
                // 成功：由 Syncer 对齐真理
                this.GetSyncer<InventorySyncer>().SyncInventoryResponse(response);

                // 发送成功表现事件
                this.SendEvent(new ItemAddedEvent { ItemId = itemId, Amount = amount });
            }
            else
            {
                string error = response?.Msg ?? "添加物品失败";
                Debug.LogError($"[InventoryService] Add item error: {error}");
                this.SendEvent(new ItemOperationFailedEvent { Reason = error });
            }
        }

        public void RequestRemoveItem(string uid, int amount)
        {
            RemoveItemAsync(uid, amount).Forget();
        }

        private async UniTaskVoid RemoveItemAsync(string uid, int amount)
        {
            Debug.Log($"[InventoryService] 申请移除物品: {uid}, 数量: {amount}");

            var response = await NetworkClient.PostAsync<object, InventoryResponse>("/inventory/remove",
                new { uid = uid, amount = amount });

            if (response != null && response.Code == 0)
            {
                this.GetSyncer<InventorySyncer>().SyncInventoryResponse(response);
                this.SendEvent(new ItemRemovedEvent { Uid = uid, Amount = amount });
            }
            else
            {
                string error = response?.Msg ?? "移除物品失败";
                Debug.LogError($"[InventoryService] Remove item error: {error}");
                this.SendEvent(new ItemOperationFailedEvent { Reason = error });
            }
        }

        public void RequestUseItem(string uid, int amount)
        {
            UseItemAsync(uid, amount).Forget();
        }

        private async UniTaskVoid UseItemAsync(string uid, int amount)
        {
            Debug.Log($"[InventoryService] 申请使用物品: {uid}");

            // 1. 发起请求
            var response = await NetworkClient.PostAsync<object, UseItemResponse>("/inventory/use",
                new { uid = uid, amount = amount });

            if (response != null && response.Code == 0)
            {
                if (response.Data != null && response.Data.Inventory != null)
                {
                    this.GetSyncer<InventorySyncer>().SyncInventoryResponse(new InventoryResponse
                    {
                        Code = 0,
                        Data = response.Data.Inventory
                    });
                }

                // 3. 播放表现（如：使用成功音效、特效）
                Debug.Log($"[InventoryService] 物品使用成功: {uid}");
                this.SendEvent(new ItemUsedEvent { Uid = uid, Amount = amount });
            }
            else
            {
                string error = response?.Msg ?? "使用物品失败";
                Debug.LogError($"[InventoryService] Use item error: {error}");
                this.SendEvent(new ItemOperationFailedEvent { Reason = error });
            }
        }
    }
}
