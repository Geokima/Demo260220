using Framework;
using Game.Base;
using Game.Consts;
using Game.DTOs;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.Inventory
{
    /// <summary>
    /// 背包代理 - 负责同步物品数据变动
    /// </summary>
    public class InventorySyncer : BaseSyncer
    {
        public override void Init()
        {
            // 监听服务器主动推送的背包更新 (如：捡起物品、奖励发放)
            RegisterWsHandler(NetworkMsgType.InventoryUpdate, OnInventoryUpdateReceived);
        }

        /// <summary>
        /// 同步 HTTP 响应中的背包全量/增量数据
        /// </summary>
        public void SyncInventoryResponse(InventoryResponse response)
        {
            if (response == null || response.Code != 0 || response.Data == null) return;
            UpdateInventoryModel(response.Data);
        }

        /// <summary>
        /// 同步 WebSocket 推送中的背包数据
        /// </summary>
        private void OnInventoryUpdateReceived(JToken data)
        {
            var inventoryData = data["inventory"]?.ToObject<InventoryDTO>();
            if (inventoryData != null)
            {
                UpdateInventoryModel(inventoryData);
            }
        }

        private void UpdateInventoryModel(InventoryDTO dto)
        {
            if (dto == null) return;


            var inventoryModel = this.GetModel<InventoryModel>();

            // 数据对齐：在商业项目中，这里通常会处理增量更新逻辑
            // 目前先全量 Set，后续可优化为 Diff 更新

            inventoryModel.SetInventory(dto);


            Debug.Log($"[InventorySyncer] 背包数据已对齐: {dto.items?.Length ?? 0} 个物品");

            // 发送内部事件，通知 UI 局部刷新（如果 Model 没做 Bindable 数组的话）

            this.SendEvent(new InventoryUpdatedEvent { Inventory = dto });
        }
    }
}
