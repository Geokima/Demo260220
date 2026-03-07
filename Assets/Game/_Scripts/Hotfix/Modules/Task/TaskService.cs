using Cysharp.Threading.Tasks;
using Framework;
using Game.Auth;
using Game.Base;
using Game.DTOs;
using Game.Inventory;
using UnityEngine;

namespace Game.Task
{
    public class TaskService : BaseService
    {
        public override void Init()
        {
            // 登录/登出事件
            this.RegisterEvent<LoginSuccessEvent>(OnLoginSuccess);
            this.RegisterEvent<LogoutEvent>(OnLogout);
            
            // 业务事件监听 - 自动追踪任务进度
            this.RegisterEvent<ItemUsedEvent>(OnItemUsed);
            // 可以继续添加其他业务事件监听（如登录、购买等）
        }

        private void OnLogout(LogoutEvent e)
        {
            this.GetModel<TaskModel>().Clear();
        }

        private async void OnLoginSuccess(LoginSuccessEvent e)
        {
            var response = await GetTaskListInternalAsync();
            if (response != null && response.Code == 0)
            {
                // 服务器响应成功后，由Syncer更新本地Model
                this.GetSyncer<TaskSyncer>().SyncTaskListResponse(response);
            }
        }

        // 事件监听器 - 物品使用事件
        private void OnItemUsed(ItemUsedEvent e)
        {
            Debug.Log($"[TaskService] 监听到物品使用事件: {e.Uid}");
            
            // Service负责HTTP请求，Syncer只负责更新
            ReportTaskProgressAsync("item_use").Forget();
        }
        
        private async UniTaskVoid ReportTaskProgressAsync(string conditionType)
        {
            var response = await NetworkClient.PostAsync<TaskProgressRequest, TaskProgressResponse>("/task/progress",
                new TaskProgressRequest { ConditionType = conditionType, Amount = 1 });
            
            if (response != null && response.Code == 0)
            {
                // 服务器确认成功，交给Syncer更新本地Model
                this.GetSyncer<TaskSyncer>().SyncTaskProgress(response);
            }
        }

        public void RequestGetTaskList()
        {
            GetTaskListInternalAsync().Forget();
        }

        private async UniTask<TaskListResponse> GetTaskListInternalAsync()
        {
            Debug.Log("[TaskService] 请求获取任务列表...");
            var response = await NetworkClient.PostAsync<TaskListResponse>("/task/list");
            return response;
        }

        public void RequestClaimTask(string taskId)
        {
            ClaimTaskInternalAsync(taskId).Forget();
        }

        private async UniTask<ClaimTaskResponse> ClaimTaskInternalAsync(string taskId)
        {
            Debug.Log($"[TaskService] 请求领取任务奖励: {taskId}");
            var response = await NetworkClient.PostAsync<ClaimTaskRequest, ClaimTaskResponse>("/task/claim",
                new ClaimTaskRequest { TaskId = taskId });

            if (response != null && response.Code == 0)
            {
                // 服务器确认领取成功后，由Syncer更新本地Model
                if (response.Tasks != null)
                {
                    this.GetSyncer<TaskSyncer>().SyncTaskList(response.Tasks);
                }
                // 发送领取成功事件
                this.SendEvent(new TaskClaimedEvent { TaskId = taskId });
            }
            else
            {
                var reason = response?.Msg ?? "领取任务奖励失败";
                Debug.LogError($"[TaskService] 领取任务奖励失败: {reason}");
                this.SendEvent(new TaskOperationFailedEvent { Reason = reason });
            }

            return response;
        }
    }
}