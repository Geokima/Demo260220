using System;
using System.Collections.Generic;
using System.Linq;
using Game.Base;
using Game.Consts;
using Game.DTOs;
using UnityEngine;

namespace Game.Gateways
{
    public static class MailController
    {
        private static readonly string[] _testTitles = { "欢迎礼包", "每日签到", "活动奖励", "系统通知", "新手礼包", "升级奖励", "节日活动", "神秘奖励" };
        private static readonly string[] _testSenders = { "系统", "管理员", "活动中心", "新手导师", "礼包发放" };
        private static readonly int[] _testItemIds = { 1001, 1002, 1003, 1004 };

        public static void GenerateAndAddTestMail(ServerContext ctx)
        {
            var rand = new System.Random();
            var title = _testTitles[rand.Next(_testTitles.Length)];
            var sender = _testSenders[rand.Next(_testSenders.Length)];
            var itemId = _testItemIds[rand.Next(_testItemIds.Length)];
            var itemCount = rand.Next(1, 6);

            var mail = new MailData
            {
                MailId = $"mail_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{rand.Next(1000, 9999)}",
                Title = title,
                Content = $"这是{title}，请查收！",
                Sender = sender,
                CreateTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                IsRead = false,
                IsReceived = false,
                Attachments = new List<ObtainItem>
                {
                    new ObtainItem { Type = ObtainType.Item, ItemId = itemId, Count = itemCount }
                }
            };

            ctx.Db.AddMail(ctx.UserId, mail);

            // 专属推送
            ctx.DirectPushAction?.Invoke(ctx.UserId, NetworkMsgType.MailUpdate, new MailSyncData
            {
                ChangedMails = new List<MailData> { mail },
                Revision = ctx.Db.GetMails(ctx.UserId).Revision
            });

            Debug.Log($"[MailController] 系统推送邮件到用户: {ctx.UserId}, 标题: {title}");
        }

        public static MailListResponse HandleGetMailList(ServerContext ctx, BaseRequest req)
        {
            return new MailListResponse
            {
                Code = 0,
                Data = ctx.Db.GetMails(ctx.UserId)
            };
        }

        public static MailSyncResponse HandleReadMail(ServerContext ctx, MailOpRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.MailId))
                return new MailSyncResponse { Code = 1, Msg = "邮件ID无效" };

            ctx.Db.MarkMailRead(ctx.UserId, req.MailId);
            var mail = ctx.Db.GetMail(ctx.UserId, req.MailId);

            return new MailSyncResponse
            {
                Code = 0,
                Data = new MailSyncData
                {
                    ChangedMails = new List<MailData> { mail },
                    Revision = ctx.Db.GetMails(ctx.UserId).Revision
                }
            };
        }

        public static MailSyncResponse HandleReceiveAttachment(ServerContext ctx, MailOpRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.MailId))
                return new MailSyncResponse { Code = 1, Msg = "邮件ID无效" };

            var mail = ctx.Db.GetMail(ctx.UserId, req.MailId);
            if (mail == null || mail.IsReceived || mail.Attachments == null || mail.Attachments.Count == 0)
                return new MailSyncResponse { Code = 2, Msg = "邮件不存在或已领取" };

            // 业务逻辑判断：背包空间是否足够
            if (!CheckInventorySpace(ctx, mail.Attachments))
                return new MailSyncResponse { Code = 3, Msg = "领取失败：背包空间不足" };

            // 1. 发放奖励
            ctx.Db.ApplyObtainItems(ctx.UserId, mail.Attachments, out var obtainResult);

            // 2. 更新邮件状态
            ctx.Db.MarkMailReceived(ctx.UserId, req.MailId);
            var updatedMail = ctx.Db.GetMail(ctx.UserId, req.MailId);
            var mailRevision = ctx.Db.GetMails(ctx.UserId).Revision;

            var syncData = new MailSyncData
            {
                ChangedMails = new List<MailData> { updatedMail },
                ObtainedItems = obtainResult.ObtainedItems,
                Revision = mailRevision
            };

            // 3. 推送背包和玩家更新
            if (obtainResult.RealChangedItems.Count > 0)
            {
                ctx.DirectPushAction?.Invoke(ctx.UserId, NetworkMsgType.InventoryUpdate, new InventorySyncData
                {
                    ChangedItems = obtainResult.RealChangedItems,
                    Reason = InventorySyncReason.MAIL,
                    Revision = ctx.Db.GetInventory(ctx.UserId).Revision
                });
            }

            if (obtainResult.PlayerDataChanged)
            {
                ctx.DirectPushAction?.Invoke(ctx.UserId, NetworkMsgType.PlayerSync, obtainResult.UpdatedPlayer);
            }

            return new MailSyncResponse { Code = 0, Data = syncData };
        }

        private static bool CheckInventorySpace(ServerContext ctx, List<ObtainItem> attachments)
        {
            var inventory = ctx.Db.GetInventory(ctx.UserId);
            var items = inventory.Items?.ToList() ?? new List<ItemData>();
            int usedSlots = items.Count;
            int maxSlots = inventory.MaxSlots;

            // 简单估算：每个新物品占用一个格子，堆叠逻辑由 AddItem 处理
            // 严谨点可以模拟 AddItem 的行为，但在 Mock 中通常做简单检查即可
            int newItemsCount = attachments.Count(a => a.Type == ObtainType.Item);
            return (usedSlots + newItemsCount) <= maxSlots;
        }

        public static MailSyncResponse HandleDeleteMail(ServerContext ctx, MailOpRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.MailId))
                return new MailSyncResponse { Code = 1, Msg = "邮件ID无效" };

            ctx.Db.DeleteMail(ctx.UserId, req.MailId, out var syncData);
            return new MailSyncResponse { Code = 0, Data = syncData };
        }
    }
}
