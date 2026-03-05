using Framework;
using Game.Base;
using Game.Consts;
using Game.DTOs;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Inventory
{
    /// <summary>
    /// 背包同步器 - 负责接收服务器推送的数据差异包并同步到模型
    /// </summary>
    public class InventorySyncer : BaseSyncer
    {
        public override void Init()
        {
            // 监听服务器端的 WebSocket 广播消息（增量同步包）
            // 无论是因为使用物品、获得掉落、还是任务奖励，后端都会推这个消息
            RegisterWsHandler(NetworkMsgType.InventoryUpdate, OnInventorySyncReceived);
        }

        /// <summary>
        /// 登录或重连成功后，同步全量背包响应包
        /// </summary>
        public void SyncInventoryResponse(InventoryResponse response)
        {
            if (response == null || response.Code != 0 || response.Data == null) return;

            // 技巧：将全量包转换为一个特殊的增量同步包 (Reason 为 LOGIN)
            var fullSyncData = new InventorySyncData
            {
                ChangedItems = new List<ItemData>(response.Data.Items),
                NewSlots = response.Data.MaxSlots,
                Reason = InventorySyncReason.LOGIN 
            };

            UpdateInventoryWithSyncData(fullSyncData);
        }

        /// <summary>
        /// 收到服务器推送的 Json 包逻辑
        /// </summary>
        private void OnInventorySyncReceived(JToken data)
        {
            var syncData = data.ToObject<InventorySyncData>();
            if (syncData != null)
            {
                UpdateInventoryWithSyncData(syncData);
            }
        }

        /// <summary>
        /// 驱动 Model 更新并分发全局同步事件
        /// </summary>
        private void UpdateInventoryWithSyncData(InventorySyncData syncData)
        {
            var model = this.GetModel<InventoryModel>();
            
            // 1. 让 Model 按照“真相绝对值”对齐数据 (O(1) 性能，只更新变动的格子)
            model.SyncDiff(syncData);

            // 2. 发送全局同步事件，通知 UI 和其他业务层
            this.SendEvent(new InventorySyncEvent 
            { 
                SyncData = syncData 
            });

            Debug.Log($"[InventorySync] 业务对齐成功. 来源:{syncData.Reason}, 变动:{syncData.ChangedItems?.Count ?? 0}, 移除:{syncData.RemovedUids?.Count ?? 0}");
        }
    }
}
