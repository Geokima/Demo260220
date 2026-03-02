using System;

namespace Framework.Modules.Timer
{
    internal class Timer : IComparable<Timer>
    {
        public int Id;
        public float TriggerTime;
        public float Interval;
        public int RepeatCount;
        public int ExecutedCount;
        public Action Callback;
        public bool IsPaused;
        public bool IsCancelled;

        public int CompareTo(Timer other)
        {
            int result = TriggerTime.CompareTo(other.TriggerTime);
            if (result == 0)
                result = Id.CompareTo(other.Id);
            return result;
        }
    }
}
