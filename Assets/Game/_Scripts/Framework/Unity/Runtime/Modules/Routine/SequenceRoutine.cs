using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Framework.Modules.Routine
{
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
}
