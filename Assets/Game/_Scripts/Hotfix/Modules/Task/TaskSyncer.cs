using Framework;
using Game.Base;
using Game.DTOs;
using Newtonsoft.Json.Linq;

namespace Game.Task
{
    public class TaskSyncer : BaseSyncer
    {
        public override void Init()
        {
            // 注册WebSocket消息处理器，用于接收服务器推送的任务更新
            RegisterWsHandler("task_update", OnTaskUpdateReceived);
        }

        public void SyncTaskListResponse(TaskListResponse response)
        {
            if (response?.Data != null)
            {
                // 服务器返回的任务列表，更新本地Model
                this.GetModel<TaskModel>().SyncAll(response.Data);
            }
        }

        public void SyncTaskList(TaskData[] tasks)
        {
            if (tasks != null)
            {
                // 更新任务列表数据
                this.GetModel<TaskModel>().SyncAll(tasks);
            }
        }

        public void SyncTaskProgress(TaskProgressResponse response)
        {
            if (response?.Code == 0)
            {
                // Service负责HTTP请求，Syncer只负责更新Model
                this.GetModel<TaskModel>().UpdateTaskProgress(response.ConditionType, response.Amount);
            }
        }

        private void OnTaskUpdateReceived(JToken data)
        {
            // 处理服务器推送的任务更新
            var taskData = data["task"]?.ToObject<TaskData>();
            if (taskData != null)
            {
                var taskModel = this.GetModel<TaskModel>();
                if (taskModel.Tasks.ContainsKey(taskData.TaskId))
                {
                    taskModel.Tasks[taskData.TaskId].Value = taskData;
                }
            }
        }
    }
}