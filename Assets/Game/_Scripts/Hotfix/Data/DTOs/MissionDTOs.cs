using System;
using Game.Base;
using Newtonsoft.Json;

namespace Game.DTOs
{
    public class MissionData
    {
        public string MissionId;
        public string Type; 
        public int CurrentProgress;
        public int TargetProgress;
        public MissionStatus Status; 
        public long LastUpdate;
        public long CreateTime;
    }

    public enum MissionStatus
    {
        InProgress = 0, // 进行中
        Completed = 1,  // 已完成，待领取 (明日方舟中的“可领取”)
        Claimed = 2     // 已领取 (明日方舟中的“已达成”)
    }

    [Serializable]
    public class MissionListData
    {
        [JsonProperty("missions")] public MissionData[] Missions;
        [JsonProperty("revision")] public long Revision;
    }

    public class MissionListResponse : BaseResponse<MissionListData> { }

    public class ClaimMissionRequest : BaseRequest { public string MissionId; }

    [Serializable]
    public class ClaimMissionData
    {
        [JsonProperty("missions")] public MissionData[] Missions;
        [JsonProperty("revision")] public long Revision;
    }

    public class ClaimMissionResponse : BaseResponse<ClaimMissionData> { }

    public class MissionProgressRequest : BaseRequest
    {
        public string ConditionType;
        public int Amount = 1;
    }

    [Serializable]
    public class MissionProgressData
    {
        [JsonProperty("updatedMissions")] public MissionData[] UpdatedMissions;
        [JsonProperty("revision")] public long Revision;
    }

    public class MissionProgressResponse : BaseResponse<MissionProgressData> { }
}