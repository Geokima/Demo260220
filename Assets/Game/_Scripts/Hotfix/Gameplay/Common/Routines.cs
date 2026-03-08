using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Game.Gameplay.Common
{
    /// <summary>
    /// 异步流程单元接口 - 任何可以被"播放并等待完成"的东西
    /// 例如：动画、音效、延时、逻辑等待，均可实现此接口并加入序列
    /// </summary>
    public interface IRoutine
    {
        UniTask RunAsync();
    }

    /// <summary>
    /// 串行序列：依次执行，上一个完成后才执行下一个
    /// </summary>
    public class SequenceRoutine : IRoutine
    {
        private readonly Queue<IRoutine> _queue = new Queue<IRoutine>();

        public SequenceRoutine Append(IRoutine routine)
        {
            if (routine != null)
                _queue.Enqueue(routine);
            return this;
        }

        public async UniTask RunAsync()
        {
            while (_queue.Count > 0)
                await _queue.Dequeue().RunAsync();
        }
    }

    /// <summary>
    /// 并行组：同时执行所有流程，等待最慢的一个完成
    /// </summary>
    public class ParallelRoutine : IRoutine
    {
        private readonly List<IRoutine> _routines = new List<IRoutine>();

        public ParallelRoutine Add(IRoutine routine)
        {
            if (routine != null)
                _routines.Add(routine);
            return this;
        }

        public async UniTask RunAsync()
        {
            var tasks = new List<UniTask>(_routines.Count);
            foreach (var routine in _routines)
                tasks.Add(routine.RunAsync());
            await UniTask.WhenAll(tasks);
        }
    }

    /// <summary>
    /// 延时流程：等待指定秒数后完成
    /// </summary>
    public class DelayRoutine : IRoutine
    {
        private readonly float _seconds;
        public DelayRoutine(float seconds) => _seconds = seconds;

        public async UniTask RunAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_seconds));
        }
    }

    /// <summary>
    /// 回调流程：执行一段同步代码后立即完成
    /// </summary>
    public class CallbackRoutine : IRoutine
    {
        private readonly Action _callback;
        public CallbackRoutine(Action callback) => _callback = callback;

        public UniTask RunAsync()
        {
            _callback?.Invoke();
            return UniTask.CompletedTask;
        }
    }
}
