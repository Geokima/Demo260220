using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Modules.Timer
{
    public class TimerSystem : AbstractSystem
    {
        private readonly TimerMinHeap _heap = new();
        private readonly Dictionary<int, Timer> _timerDict = new();
        private int _nextId = 1;
        private TimerDriver _driver;
        private float _pauseTime;
        private bool _isPaused;

        public override void Init()
        {
            var go = new GameObject("TimerSystem");
            UnityEngine.Object.DontDestroyOnLoad(go);
            _driver = go.AddComponent<TimerDriver>();
            _driver.OnUpdate = Update;
        }

        public override void Deinit()
        {
            if (_driver != null)
                UnityEngine.Object.Destroy(_driver.gameObject);
            _heap.Clear();
            _timerDict.Clear();
        }

        public int Delay(float seconds, Action callback)
        {
            return AddTimer(Time.time + seconds, seconds, callback, 1);
        }

        public int Interval(float seconds, Action callback, int repeatCount = -1)
        {
            return AddTimer(Time.time + seconds, seconds, callback, repeatCount);
        }

        public void Cancel(int timerId)
        {
            if (_timerDict.TryGetValue(timerId, out var timer))
            {
                timer.IsCancelled = true;
                _timerDict.Remove(timerId);
            }
        }

        public void Pause(int timerId)
        {
            if (_timerDict.TryGetValue(timerId, out var timer))
                timer.IsPaused = true;
        }

        public void Resume(int timerId)
        {
            if (_timerDict.TryGetValue(timerId, out var timer) && timer.IsPaused)
            {
                timer.IsPaused = false;
                timer.TriggerTime = Time.time + timer.Interval;
            }
        }

        public void PauseAll()
        {
            if (_isPaused) return;
            _isPaused = true;
            _pauseTime = Time.time;
            foreach (var timer in _timerDict.Values)
                timer.IsPaused = true;
        }

        public void ResumeAll()
        {
            if (!_isPaused) return;
            _isPaused = false;
            float delta = Time.time - _pauseTime;
            foreach (var timer in _timerDict.Values)
            {
                timer.IsPaused = false;
                timer.TriggerTime += delta;
            }
        }

        public void CancelAll()
        {
            _heap.Clear();
            _timerDict.Clear();
        }

        public float GetRemaining(int timerId)
        {
            if (_timerDict.TryGetValue(timerId, out var timer))
                return Mathf.Max(0, timer.TriggerTime - Time.time);
            return 0f;
        }

        public string GetRemainingString(int timerId)
        {
            float remaining = GetRemaining(timerId);
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            return $"{minutes:D2}:{seconds:D2}";
        }

        public bool IsRunning(int timerId)
        {
            return _timerDict.ContainsKey(timerId);
        }

        public int GetActiveCount()
        {
            return _timerDict.Count;
        }

        private void Update()
        {
            if (_isPaused) return;

            while (_heap.Count > 0)
            {
                var timer = _heap.Peek();
                if (timer.IsCancelled || timer.IsPaused)
                {
                    _heap.Pop();
                    continue;
                }

                if (timer.TriggerTime > Time.time)
                    break;

                _heap.Pop();

                try
                {
                    timer.Callback?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TimerSystem] Timer {timer.Id} exception: {e}");
                }

                timer.ExecutedCount++;

                if (timer.RepeatCount < 0 || timer.ExecutedCount < timer.RepeatCount)
                {
                    timer.TriggerTime = Time.time + timer.Interval;
                    _heap.Push(timer);
                }
                else
                {
                    _timerDict.Remove(timer.Id);
                }
            }
        }

        private int AddTimer(float triggerTime, float interval, Action callback, int repeatCount)
        {
            var id = _nextId++;
            var timer = new Timer
            {
                Id = id,
                TriggerTime = triggerTime,
                Interval = interval,
                RepeatCount = repeatCount,
                Callback = callback
            };
            _heap.Push(timer);
            _timerDict[id] = timer;
            return id;
        }
    }
}
