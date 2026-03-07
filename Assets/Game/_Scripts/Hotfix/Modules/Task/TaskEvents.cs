using System;

namespace Game.Task
{
    public class TaskClaimedEvent
    {
        public string TaskId;
    }

    public class TaskCompletedEvent
    {
        public string TaskId;
    }

    public class TaskOperationFailedEvent
    {
        public string Reason;
    }
}