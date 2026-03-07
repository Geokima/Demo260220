using Framework;
using Game.DTOs;

namespace Game.Task
{
    public class TaskModel : AbstractModel
    {
        // 使用 BindableDictionary 管理任务数据，保持与 InventoryModel 一致
        public BindableDictionary<string, BindableProperty<TaskData>> Tasks = new();
        public BindableProperty<long> Revision = new BindableProperty<long>(0);

        public void SyncAll(TaskData[] tasks)
        {
            Tasks.Clear();
            if (tasks != null)
            {
                foreach (var task in tasks)
                {
                    Tasks[task.TaskId] = new BindableProperty<TaskData>(task);
                }
            }
        }

        public void UpdateTaskProgress(string taskId, int newProgress)
        {
            if (Tasks.TryGetValue(taskId, out var taskProp))
            {
                var oldTask = taskProp.Value;
                var newTask = new TaskData
                {
                    TaskId = oldTask.TaskId,
                    Type = oldTask.Type,
                    CurrentProgress = newProgress,
                    TargetProgress = oldTask.TargetProgress,
                    Status = newProgress >= oldTask.TargetProgress ? TaskStatus.Completed : TaskStatus.InProgress,
                    LastUpdate = System.DateTime.Now.Ticks,
                    CreateTime = oldTask.CreateTime
                };
                taskProp.Value = newTask;
            }
        }

        public void IncrementTaskProgress(string taskId, int amount = 1)
        {
            if (Tasks.TryGetValue(taskId, out var taskProp))
            {
                var oldTask = taskProp.Value;
                var newProgress = oldTask.CurrentProgress + amount;
                UpdateTaskProgress(taskId, newProgress);
            }
        }

        public void MarkAsClaimed(string taskId)
        {
            if (Tasks.TryGetValue(taskId, out var taskProp))
            {
                var oldTask = taskProp.Value;
                if (oldTask.Status == TaskStatus.Completed)
                {
                    var newTask = new TaskData
                    {
                        TaskId = oldTask.TaskId,
                        Type = oldTask.Type,
                        CurrentProgress = oldTask.CurrentProgress,
                        TargetProgress = oldTask.TargetProgress,
                        Status = TaskStatus.Claimed,
                        LastUpdate = System.DateTime.Now.Ticks,
                        CreateTime = oldTask.CreateTime
                    };
                    taskProp.Value = newTask;
                }
            }
        }

        public void Clear()
        {
            Tasks.Clear();
        }
    }
}