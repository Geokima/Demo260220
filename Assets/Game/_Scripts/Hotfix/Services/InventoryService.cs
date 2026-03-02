using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Http;
using Game.Data;
using Game.Models;
using UnityEngine;

namespace Game.Services
{
    public class InventoryService : AbstractSystem
    {
        private IHttpSystem _httpSystem;

        public override void Init()
        {
            _httpSystem = this.GetSystem<IHttpSystem>();
        }

        public void RequestGetInventory()
        {
            GetInventoryAsync().Forget();
        }

        private async UniTaskVoid GetInventoryAsync()
        {
            var accountModel = this.GetModel<AccountModel>();
            if (string.IsNullOrEmpty(accountModel.Token.Value))
            {
                Debug.LogError("[InventoryService] Not logged in");
                return;
            }

            var json = $"{{\"token\":\"{accountModel.Token.Value}\"}}";
            var response = await _httpSystem.PostAsync("/inventory/get", json);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[InventoryService] Get inventory failed");
                return;
            }

            var result = JsonUtility.FromJson<InventoryResponse>(response);
            if (result.code == 0)
            {
                var inventoryModel = this.GetModel<InventoryModel>();
                inventoryModel.SetInventory(result.inventory);
                this.SendEvent(new InventoryUpdatedEvent { Inventory = result.inventory });
            }
            else
            {
                Debug.LogError($"[InventoryService] Get inventory error: {result.msg}");
            }
        }

        public void RequestAddItem(int itemId, int amount, bool bind = false)
        {
            AddItemAsync(itemId, amount, bind).Forget();
        }

        private async UniTaskVoid AddItemAsync(int itemId, int amount, bool bind)
        {
            var accountModel = this.GetModel<AccountModel>();
            if (string.IsNullOrEmpty(accountModel.Token.Value))
            {
                Debug.LogError("[InventoryService] Not logged in");
                return;
            }

            var json = $"{{\"token\":\"{accountModel.Token.Value}\",\"itemId\":{itemId},\"amount\":{amount},\"bind\":{bind.ToString().ToLower()}}}";
            var response = await _httpSystem.PostAsync("/inventory/add", json);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[InventoryService] Add item failed");
                return;
            }

            var result = JsonUtility.FromJson<InventoryResponse>(response);
            if (result.code == 0)
            {
                var inventoryModel = this.GetModel<InventoryModel>();
                inventoryModel.SetInventory(result.inventory);
                this.SendEvent(new ItemAddedEvent { ItemId = itemId, Amount = amount });
            }
            else
            {
                Debug.LogError($"[InventoryService] Add item error: {result.msg}");
                this.SendEvent(new ItemOperationFailedEvent { Reason = result.msg });
            }
        }

        public void RequestRemoveItem(string uid, int amount)
        {
            RemoveItemAsync(uid, amount).Forget();
        }

        private async UniTaskVoid RemoveItemAsync(string uid, int amount)
        {
            var accountModel = this.GetModel<AccountModel>();
            if (string.IsNullOrEmpty(accountModel.Token.Value))
            {
                Debug.LogError("[InventoryService] Not logged in");
                return;
            }

            var json = $"{{\"token\":\"{accountModel.Token.Value}\",\"uid\":\"{uid}\",\"amount\":{amount}}}";
            var response = await _httpSystem.PostAsync("/inventory/remove", json);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[InventoryService] Remove item failed");
                return;
            }

            var result = JsonUtility.FromJson<InventoryResponse>(response);
            if (result.code == 0)
            {
                var inventoryModel = this.GetModel<InventoryModel>();
                inventoryModel.SetInventory(result.inventory);
                this.SendEvent(new ItemRemovedEvent { Uid = uid, Amount = amount });
            }
            else
            {
                Debug.LogError($"[InventoryService] Remove item error: {result.msg}");
                this.SendEvent(new ItemOperationFailedEvent { Reason = result.msg });
            }
        }

        public void RequestUseItem(string uid, int amount)
        {
            UseItemAsync(uid, amount).Forget();
        }

        private async UniTaskVoid UseItemAsync(string uid, int amount)
        {
            var accountModel = this.GetModel<AccountModel>();
            if (string.IsNullOrEmpty(accountModel.Token.Value))
            {
                Debug.LogError("[InventoryService] Not logged in");
                return;
            }

            var json = $"{{\"token\":\"{accountModel.Token.Value}\",\"uid\":\"{uid}\",\"amount\":{amount}}}";
            var response = await _httpSystem.PostAsync("/inventory/use", json);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[InventoryService] Use item failed");
                return;
            }

            var result = JsonUtility.FromJson<UseItemResponse>(response);
            if (result.code == 0)
            {
                var inventoryModel = this.GetModel<InventoryModel>();
                inventoryModel.SetInventory(result.inventory);
                this.SendEvent(new ItemUsedEvent { Uid = uid, Amount = amount, Effect = result.effect });
            }
            else
            {
                Debug.LogError($"[InventoryService] Use item error: {result.msg}");
                this.SendEvent(new ItemOperationFailedEvent { Reason = result.msg });
            }
        }

        [System.Serializable]
        private class InventoryResponse
        {
            public int code;
            public string msg;
            public InventoryData inventory;
        }

        [System.Serializable]
        private class UseItemResponse
        {
            public int code;
            public string msg;
            public InventoryData inventory;
            public ItemEffect effect;
        }
    }
}
