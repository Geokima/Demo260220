using System;
using System.Collections.Generic;
using System.Linq;
using Game.Base;
using Game.Config;
using Game.Consts;
using Game.DTOs;
using UnityEngine;

namespace Game.Gateways
{
    public static class MissionController
    {
        public static MissionListResponse HandleGetMissions(ServerContext ctx, BaseRequest req)
        {
            var missionList = ctx.Db.GetMissions(ctx.UserId);
            if (CheckAndInitMissions(ctx, ref missionList))
            {
                ctx.Db.UpdateMissions(ctx.UserId, missionList);
            }
            return new MissionListResponse { Code = 0, Data = missionList };
        }

        public static ClaimMissionResponse HandleClaimMission(ServerContext ctx, ClaimMissionRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.MissionId))
                return new ClaimMissionResponse { Code = 1, Msg = "Mission ID invalid" };

            var missionList = ctx.Db.GetMissions(ctx.UserId);
            var missions = missionList.Missions?.ToList() ?? new List<MissionData>();
            var mission = missions.FirstOrDefault(m => m.MissionId == req.MissionId);

            if (mission == null)
                return new ClaimMissionResponse { Code = 2, Msg = "Mission not found" };

            if (mission.Status != MissionStatus.Completed)
                return new ClaimMissionResponse { Code = 3, Msg = "Mission not completed or already claimed" };

            var config = ctx.Configs.Get<MissionConfig>(int.Parse(req.MissionId));
            if (config != null && config.Rewards != null)
            {
                ctx.Db.ApplyObtainItems(ctx.UserId, config.Rewards, out var obtainResult);
                if (obtainResult.RealChangedItems.Count > 0)
                {
                    ctx.DirectPushAction?.Invoke(ctx.UserId, NetworkMsgType.InventoryUpdate, new InventorySyncData
                    {
                        ChangedItems = obtainResult.RealChangedItems,
                        Reason = InventorySyncReason.MISSION,
                        Revision = ctx.Db.GetInventory(ctx.UserId).Revision
                    });
                }
                if (obtainResult.PlayerDataChanged)
                {
                    ctx.DirectPushAction?.Invoke(ctx.UserId, NetworkMsgType.PlayerSync, obtainResult.UpdatedPlayer);
                }
            }

            mission.Status = MissionStatus.Claimed;
            mission.LastUpdate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            missionList.Revision++;
            missionList.Missions = missions.ToArray();
            ctx.Db.UpdateMissions(ctx.UserId, missionList);

            return new ClaimMissionResponse 
            { 
                Code = 0, 
                Data = new ClaimMissionData { Missions = missionList.Missions, Revision = missionList.Revision } 
            };
        }

        public static MissionProgressResponse HandleProgress(ServerContext ctx, MissionProgressRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.ConditionType))
                return new MissionProgressResponse { Code = 1, Msg = "Invalid condition" };

            var missionList = ctx.Db.GetMissions(ctx.UserId);
            var missions = missionList.Missions;
            if (missions == null) return new MissionProgressResponse { Code = 0 };

            bool changed = false;
            List<MissionData> updatedMissions = new List<MissionData>();

            foreach (var mission in missions)
            {
                if (mission.Status != MissionStatus.InProgress) continue;
                var config = ctx.Configs.Get<MissionConfig>(int.Parse(mission.MissionId));
                if (config != null && config.ConditionType == req.ConditionType)
                {
                    mission.CurrentProgress += req.Amount;
                    if (mission.CurrentProgress >= mission.TargetProgress)
                    {
                        mission.CurrentProgress = mission.TargetProgress;
                        mission.Status = MissionStatus.Completed;
                    }
                    mission.LastUpdate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    updatedMissions.Add(mission);
                    changed = true;
                }
            }

            if (changed)
            {
                missionList.Revision++;
                ctx.Db.UpdateMissions(ctx.UserId, missionList);
                ctx.DirectPushAction?.Invoke(ctx.UserId, NetworkMsgType.MissionUpdate, missionList);
            }

            return new MissionProgressResponse
            {
                Code = 0,
                Data = new MissionProgressData { UpdatedMissions = updatedMissions.ToArray(), Revision = missionList.Revision }
            };
        }

        private static bool CheckAndInitMissions(ServerContext ctx, ref MissionListData missionList)
        {
            var configSheet = ctx.Configs.GetSheet<MissionConfig>();
            if (configSheet == null) return false;

            var configs = configSheet.All();
            var currentMissions = missionList.Missions?.ToList() ?? new List<MissionData>();
            bool changed = false;

            foreach (var config in configs)
            {
                if (!currentMissions.Any(m => m.MissionId == config.MissionId))
                {
                    currentMissions.Add(new MissionData
                    {
                        MissionId = config.MissionId,
                        Type = config.Type,
                        CurrentProgress = 0,
                        TargetProgress = config.TargetProgress,
                        Status = MissionStatus.InProgress,
                        CreateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        LastUpdate = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    });
                    changed = true;
                }
            }

            if (changed)
            {
                missionList.Missions = currentMissions.ToArray();
                missionList.Revision++;
            }
            return changed;
        }
    }
}
