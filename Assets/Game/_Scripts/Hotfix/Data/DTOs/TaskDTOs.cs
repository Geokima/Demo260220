using System;
using Newtonsoft.Json;

namespace Game.DTOs
{
    public class TaskData
    {
        public string TaskId;
        public string Type; // daily/mainline
        public int CurrentProgress;
        public int TargetProgress;
        public TaskStatus Status; // 未完成/已完成/已领取
        public long LastUpdate;
        public long CreateTime;
    }

    public enum TaskStatus
    {
        InProgress = 0,
        Completed = 1,
        Claimed = 2
    }

    [Serializable]
    public class TaskListData
    {
        [JsonProperty("tasks")]
        public TaskData[] Tasks;
    }

    public class TaskListResponse : BaseResponse<TaskListData>
    {
    }

    public class ClaimTaskRequest
    {
        public string TaskId;
    }

    [Serializable]
    public class ClaimTaskData
    {
        [JsonProperty("tasks")]
        public TaskData[] Tasks;
    }

    public class ClaimTaskResponse : BaseResponse<ClaimTaskData>
    {
    }

    public class TaskProgressRequest
    {
        public string ConditionType;
        public int Amount = 1;
    }

    [Serializable]
    public class TaskProgressData
    {
        [JsonProperty("updatedTasks")]
        public TaskData[] UpdatedTasks;
    }

    public class TaskProgressResponse : BaseResponse<TaskProgressData>
    {
    }
}