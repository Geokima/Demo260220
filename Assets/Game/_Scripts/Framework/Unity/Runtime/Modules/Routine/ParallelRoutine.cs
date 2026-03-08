using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Framework.Modules.Routine
{
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
}
