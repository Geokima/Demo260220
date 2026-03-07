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
        public void SyncGetInventoryResponse(GetInventoryResponse response)
        {
            if (response == null || response.Code != 0 || response.Data == null) return;

            Debug.Log($"[InventorySync] 收到全量同步包. Revision: {response.Data.Revision}");
            var model = this.GetModel<InventoryModel>();
            if (model.SyncAll(response.Data))
            {
                this.SendEvent(new InventorySyncEvent 
                { 
                    SyncData = new InventorySyncData 
                    { 
                        Reason = InventorySyncReason.LOGIN, 
                        Revision = response.Data.Revision 
                    } 
                });
            }
        }

        /// <summary>
        /// 处理操作后的增量同步响应
        /// </summary>
        public void SyncInventoryResponse(InventoryResponse response)
        {
            if (response == null || response.Code != 0 || response.Data == null) return;

            Debug.Log($"[InventorySync] 收到操作增量同步包. Reason: {response.Data.Reason}, Rev: {response.Data.Revision}");
            UpdateInventoryWithSyncData(response.Data);
        }

        /// <summary>
        /// 收到服务器推送的 Json 包逻辑
        /// </summary>
        private void OnInventorySyncReceived(JToken data)
        {
            Debug.Log($"[InventorySync] 收到增量同步包: {data}");
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
            
            // 1. 让 Model 按照"真相绝对值"对齐数据 (O(1) 性能，只更新变动的格子)
            // 2. 检查 SyncDiff 返回值，只有当数据真正发生变化时才发送事件
            bool hasChanged = model.SyncDiff(syncData);
            
            if (hasChanged)
            {
                // 3. 发送全局同步事件，通知 UI 和其他业务层
                this.SendEvent(new InventorySyncEvent 
                { 
                    SyncData = syncData 
                });

                Debug.Log($"[InventorySync] 业务对齐成功. 来源:{syncData.Reason}, 变动:{syncData.ChangedItems?.Count ?? 0}, 移除:{syncData.RemovedUids?.Count ?? 0}");
            }
            else
            {
                Debug.Log($"[InventorySync] 数据未发生变化，跳过事件发送. 来源:{syncData.Reason}, Revision:{syncData.Revision}");
            }
        }
    }
}
