using System;
using System.Collections.Generic;
using Game.Base;
using Game.Consts;
using Game.DTOs;
using UnityEngine;

namespace Game.Gateways
{
    public static class InventoryController
    {
        public static GetInventoryResponse HandleGetInventory(ServerContext ctx, BaseRequest req)
        {
            return new GetInventoryResponse
            {
                Code = 0,
                Data = ctx.Db.GetInventory(ctx.UserId)
            };
        }

        public static InventoryResponse HandleAddItem(ServerContext ctx, AddItemRequest req)
        {
            if (req == null)
                return new InventoryResponse { Code = (int)ErrorCode.InvalidParams, Msg = "请求无效" };

            // 1. 构建 ObtainItem 列表
            var items = new List<ObtainItem>
            {
                new ObtainItem 
                { 
                    Type = (req.ItemId <= 2) ? ObtainType.Currency : ObtainType.Item, 
                    ItemId = req.ItemId, 
                    Count = req.Amount 
                }
            };

            // 2. 使用数据库的发放能力
            if (!ctx.Db.ApplyObtainItems(ctx.UserId, items, out var result))
            {
                return new InventoryResponse { Code = (int)ErrorCode.InventoryFull, Msg = "背包已满" };
            }

            // 3. 推送更新
            if (result.RealChangedItems.Count > 0)
            {
                ctx.DirectPushAction?.Invoke(ctx.UserId, NetworkMsgType.InventoryUpdate, new InventorySyncData
                {
                    ChangedItems = result.RealChangedItems,
                    Reason = InventorySyncReason.DROP,
                    Revision = ctx.Db.GetInventory(ctx.UserId).Revision
                });
            }

            int actualAdded = result.ObtainedItems.Count > 0 ? result.ObtainedItems[0].Count : 0;
            int remaining = req.Amount - actualAdded;

            return new InventoryResponse
            {
                Code = remaining > 0 ? 2 : 0,
                Msg = remaining > 0 ? "背包已满" : "Success",
                Data = new InventorySyncData
                {
                    ChangedItems = result.RealChangedItems,
                    Reason = InventorySyncReason.DROP,
                    Revision = ctx.Db.GetInventory(ctx.UserId).Revision
                }
            };
        }

        public static InventoryResponse HandleRemoveItem(ServerContext ctx, RemoveItemRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.Uid))
                return new InventoryResponse { Code = (int)ErrorCode.InvalidParams, Msg = "请求无效" };

            ctx.Db.RemoveItem(ctx.UserId, req.Uid, req.Amount, out var updatedItem, out var removedUid);
            ctx.Db.IncrementInventoryRevision(ctx.UserId);

            var syncData = new InventorySyncData
            {
                ChangedItems = updatedItem != null ? new List<ItemData> { updatedItem } : null,
                RemovedUids = !string.IsNullOrEmpty(removedUid) ? new List<string> { removedUid } : null,
                Reason = InventorySyncReason.USE,
                Revision = ctx.Db.GetInventory(ctx.UserId).Revision
            };
            ctx.DirectPushAction?.Invoke(ctx.UserId, NetworkMsgType.InventoryUpdate, syncData);

            return new InventoryResponse
            {
                Code = 0,
                Msg = "Success",
                Data = syncData
            };
        }

        public static UseItemResponse HandleUseItem(ServerContext ctx, UseItemRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.Uid))
                return new UseItemResponse { Code = (int)ErrorCode.InvalidParams, Msg = "请求无效" };

            ctx.Db.UseItem(ctx.UserId, req.Uid, req.Amount, out var updatedItem, out var effects);
            ctx.Db.IncrementInventoryRevision(ctx.UserId);

            var syncData = new InventorySyncData
            {
                ChangedItems = updatedItem != null ? new List<ItemData> { updatedItem } : null,
                RemovedUids = (updatedItem == null && string.IsNullOrEmpty(req.Uid) == false) ? new List<string> { req.Uid } : null, // 修正此处逻辑
                Reason = InventorySyncReason.USE,
                Revision = ctx.Db.GetInventory(ctx.UserId).Revision
            };
            
            // 如果物品扣完了，uid 记得放入 RemovedUids
            if (updatedItem == null) 
                syncData.RemovedUids = new List<string> { req.Uid };
            
            ctx.DirectPushAction?.Invoke(ctx.UserId, NetworkMsgType.InventoryUpdate, syncData);

            return new UseItemResponse
            {
                Code = 0,
                Msg = "Success",
                Data = effects
            };
        }
    }
}
