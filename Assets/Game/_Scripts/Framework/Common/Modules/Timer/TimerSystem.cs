using System;
using System.Collections.Generic;
using Framework;

namespace Framework.Modules.Timer
{
    /// <summary>
    /// 计时器系统实现类
    /// </summary>
    public class TimerSystem : AbstractSystem, ITimerSystem, IUpdateable
    {
        #region Fields

        private readonly TimerMinHeap _timerMinHeap = new TimerMinHeap();
        private readonly Dictionary<int, Timer> _timerDict = new Dictionary<int, Timer>();
        private int _timerIdCounter;
        private ITimeProvider _timeProvider;

        #endregion

        #region Lifecycle

        /// <inheritdoc />
        public override void Init()
        {
            _timeProvider = Architecture.GetSystem<ITimeProvider>();
        }

        /// <inheritdoc />
        public override void Deinit()
        {
            _timerDict.Clear();
            _timerMinHeap.Clear();
        }

        /// <inheritdoc />
        public void OnUpdate()
        {
            if (_timeProvider == null) return;

            float now = _timeProvider.Time;
            while (_timerMinHeap.Count > 0 && _timerMinHeap.Peek().TriggerTime <= now)
            {
                var timer = _timerMinHeap.Pop();
                if (timer.IsCancelled) continue;

                timer.Callback?.Invoke();
                timer.ExecutedCount++;

                if (timer.RepeatCount > 0)
                {
                    if (timer.ExecutedCount < timer.RepeatCount)
                    {
                        timer.TriggerTime = now + timer.Interval;
                        _timerMinHeap.Push(timer);
                    }
                    else
                    {
                        _timerDict.Remove(timer.Id);
                    }
                }
                else if (timer.RepeatCount == -1) // 无限循环
                {
                    timer.TriggerTime = now + timer.Interval;
                    _timerMinHeap.Push(timer);
                }
                else // RepeatCount == 0 (只执行一次) 或已完成
                {
                    _timerDict.Remove(timer.Id);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public int AddTimer(float delay, Action callback)
        {
            return AddTimer(delay, 1, callback);
        }

        /// <inheritdoc />
        public int AddTimer(float delay, int loop, Action callback)
        {
            _timerIdCounter++;
            var timer = new Timer
            {
                Id = _timerIdCounter,
                Interval = delay,
                RepeatCount = loop,
                Callback = callback,
                TriggerTime = _timeProvider.Time + delay,
                ExecutedCount = 0
            };

            _timerDict.Add(timer.Id, timer);
            _timerMinHeap.Push(timer);
            return timer.Id;
        }

        /// <inheritdoc />
        public void RemoveTimer(int timerId)
        {
            if (_timerDict.TryGetValue(timerId, out var timer))
            {
                timer.IsCancelled = true;
                _timerDict.Remove(timerId);
            }
        }

        #endregion
    }
}
