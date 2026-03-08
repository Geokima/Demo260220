using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Config;
using Game.Auth;
using Game.Base;
using Game.Config;
using Game.DTOs;
using Game.Effect;
using UnityEngine;

namespace Game.Inventory
{
    public class InventoryService : BaseService
    {
        public override void Init()
        {
            this.RegisterEvent<LoginSuccessEvent>(OnLoginSuccess);
            this.RegisterEvent<LogoutEvent>(OnLogout);
        }

        private void OnLogout(LogoutEvent e)
        {
            this.GetModel<InventoryModel>().Clear();
        }

        private async void OnLoginSuccess(LoginSuccessEvent e)
        {
            var response = await GetInventoryInternalAsync();
            if (response != null && response.Code == 0)
            {
                this.GetSyncer<InventorySyncer>().SyncGetInventoryResponse(response);
            }
        }

        public async UniTask<GetInventoryResponse> GetInventoryAsync()
        {
            return await GetInventoryInternalAsync();
        }

        public void RequestGetInventory()
        {
            GetInventoryAsync().Forget();
        }

        private async UniTask<GetInventoryResponse> GetInventoryInternalAsync()
        {
            Debug.Log("[InventoryService] 请求获取全量背包...");

            var response = await ServerGateway.PostAsync<GetInventoryResponse>("/inventory/get");

            if (response != null && response.Code == 0)
            {
                this.GetSyncer<InventorySyncer>().SyncGetInventoryResponse(response);
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

            var response = await ServerGateway.PostAsync<object, InventoryResponse>("/inventory/add",
                new AddItemRequest { ItemId = itemId, Amount = amount });

            if (response != null && response.Code == 0)
            {
                // 成功：由 Syncer 对齐真理。在真实项目中，服务器可能推的是全量页(SyncInventoryResponse)
                // 也可能是通过 WebSocket 推的差异包。如果是局部成功的回报带了全量，这里依然支持。
                this.GetSyncer<InventorySyncer>().SyncInventoryResponse(response);
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

            var response = await ServerGateway.PostAsync<object, InventoryResponse>("/inventory/remove",
                new RemoveItemRequest { Uid = uid, Amount = amount });

            if (response != null && response.Code == 0)
            {
                this.GetSyncer<InventorySyncer>().SyncInventoryResponse(response);
            }
            else
            {
                string error = response?.Msg ?? "移除物品失败";
                Debug.LogError($"[InventoryService] Remove item error: {error}");
                this.SendEvent(new ItemOperationFailedEvent { Reason = error });
            }
        }

        public void RequestUseItem(string uid, int amount, Dictionary<string, string> parameters)
        {
            UseItemAsync(uid, amount, parameters).Forget();
        }

        private async UniTaskVoid UseItemAsync(string uid, int amount, Dictionary<string, string> parameters)
        {
            Debug.Log($"[InventoryService] 申请使用物品: {uid}");

            var response = await ServerGateway.PostAsync<UseItemRequest, UseItemResponse>("/inventory/use", 
                new UseItemRequest { Uid = uid, Amount = amount, Params = parameters });

            if (response != null && response.Code == 0)
            {
                Debug.Log($"[InventoryService] 物品使用成功: {uid}");
                if (response.Data != null && response.Data.Count > 0)
                {
                    foreach (var effect in response.Data)
                        this.GetSystem<EffectSystem>().Execute(effect.EffectId, effect.Params);
                }
                this.SendEvent(new ItemUsedEvent { Uid = uid, Amount = amount, Effects = response.Data });
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
