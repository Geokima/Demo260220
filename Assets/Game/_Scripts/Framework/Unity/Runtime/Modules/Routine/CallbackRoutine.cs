using System;
using Cysharp.Threading.Tasks;

namespace Framework.Modules.Routine
{
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
