using System;
using System.Collections.Generic;
using Game.Config;
using Game.DTOs;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Gateways
{
    /// <summary> 后端效果执行器 - 负责真理判定与数据库修改 </summary>
    public static class EffectProcessor
    {
        /// <summary> 执行效果并返回获得的实物列表 </summary>
        public static void Execute(ServerContext ctx, int effectId, int amount, Dictionary<string, string> requestParams, out List<ObtainItem> totalObtained, out bool playerChanged)
        {
            totalObtained = new List<ObtainItem>();
            playerChanged = false;
            var config = ctx.Configs.Get<EffectConfig>(effectId);
            if (config == null) return;

            // 1. 合并参数
            var finalParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(config.Params ?? "{}");
            if (requestParams != null)
            {
                foreach (var kv in requestParams) finalParams[kv.Key] = kv.Value;
            }

            // 2. 根据模式执行（简单累加逻辑，适合目前的增量效果）
            if (config.Type == "AddGold")
            {
                if (int.TryParse(finalParams.GetValueOrDefault("gold", "0"), out var gold))
                    totalObtained.Add(new ObtainItem { Type = "currency", ItemId = 1, Count = gold * amount });
            }
            else if (config.Type == "AddEnergy")
            {
                if (int.TryParse(finalParams.GetValueOrDefault("energy", "0"), out var energy))
                {
                    var p = ctx.Db.GetPlayer(ctx.UserId);
                    p.Energy += energy * amount;
                    ctx.Db.UpdatePlayer(ctx.UserId, p);
                    playerChanged = true;
                }
            }
            else if (config.Type == "AddExp")
            {
                if (int.TryParse(finalParams.GetValueOrDefault("exp", "0"), out var exp))
                {
                    var p = ctx.Db.GetPlayer(ctx.UserId);
                    p.Exp += exp * amount;
                    ctx.Db.UpdatePlayer(ctx.UserId, p);
                    playerChanged = true;
                }
            }
            // TODO 测试用效果
            else if (config.Type == "SpendEnergy")
            {
                if (int.TryParse(finalParams.GetValueOrDefault("energy", "0"), out var energy))
                {
                    var p = ctx.Db.GetPlayer(ctx.UserId);
                    p.Energy = Math.Max(0, p.Energy - energy * amount);
                    ctx.Db.UpdatePlayer(ctx.UserId, p);
                    playerChanged = true;
                }
            }
            else if (config.Type == "SpendGold")
            {
                if (int.TryParse(finalParams.GetValueOrDefault("gold", "0"), out var gold))
                {
                    var p = ctx.Db.GetPlayer(ctx.UserId);
                    p.Gold = Math.Max(0, p.Gold - gold * amount);
                    ctx.Db.UpdatePlayer(ctx.UserId, p);
                    playerChanged = true;
                }
            }
            else if (config.Type == "ChangeName")
            {
                if (finalParams.TryGetValue("newName", out var newName) && !string.IsNullOrEmpty(newName))
                {
                    var p = ctx.Db.GetPlayer(ctx.UserId);
                    p.Username = newName;
                    ctx.Db.UpdatePlayer(ctx.UserId, p);
                    playerChanged = true;
                }
            }

            // 3. 通用资源发放（金币/钻石走这里不会重复，因为上面只是加了列表没加数值）
            if (totalObtained.Count > 0)
            {
                ctx.Db.ApplyObtainItems(ctx.UserId, totalObtained, out var res);
                if (res.PlayerDataChanged) playerChanged = true;
            }
        }
    }
}
