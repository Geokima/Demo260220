using System;
using System.Collections.Generic;
using Game.Base;
using Game.Consts;
using Game.DTOs;
using Game.Config;
using System.Linq;
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
            if (req == null || string.IsNullOrEmpty(req.Uid) || req.Amount <= 0)
                return new UseItemResponse { Code = (int)ErrorCode.InvalidParams, Msg = "请求无效" };

            // 1. 基础校验
            var inventory = ctx.Db.GetInventory(ctx.UserId);
            var itemData = inventory.Items?.ToList().Find(i => i.Uid == req.Uid);
            if (itemData == null)
                return new UseItemResponse { Code = (int)ErrorCode.ItemNotFound, Msg = "物品不存在" };

            var config = ctx.Configs.Get<ItemConfig>(itemData.ItemId);
            if (config == null || config.Type != "Consumable") 
                return new UseItemResponse { Code = (int)ErrorCode.Failed, Msg = "该物品不可使用" };

            // 2. 扣除消耗
            if (!ctx.Db.RemoveItem(ctx.UserId, req.Uid, req.Amount, out var updatedItem, out _))
                return new UseItemResponse { Code = (int)ErrorCode.ItemCountInsufficient, Msg = "物品数量不足" };

            ctx.Db.IncrementInventoryRevision(ctx.UserId);

            // 3. 执行效果（由处理器负责修改数据库并产生奖励）
            EffectProcessor.Execute(ctx, config.EffectId, req.Amount, req.Params, out var obtainedItems, out var playerChanged);

            // 4. 下发同步包
            if (playerChanged)
            {
                var player = ctx.Db.GetPlayer(ctx.UserId);
                ctx.DirectPushAction?.Invoke(ctx.UserId, NetworkMsgType.PlayerSync, player);
            }

            var syncData = new InventorySyncData
            {
                ChangedItems = updatedItem != null ? new List<ItemData> { updatedItem } : null,
                RemovedUids = updatedItem == null ? new List<string> { req.Uid } : null,
                Reason = InventorySyncReason.USE,
                Revision = ctx.Db.GetInventory(ctx.UserId).Revision
            };
            ctx.DirectPushAction?.Invoke(ctx.UserId, NetworkMsgType.InventoryUpdate, syncData);

            return new UseItemResponse
            {
                Code = 0,
                Msg = "Success",
                Data = new UseItemResponseData
                {
                    ObtainedItems = obtainedItems,
                    Effects = new List<ItemEffect> { new ItemEffect { EffectId = config.EffectId, Params = req.Params } }
                }
            };
        }
    }
}
