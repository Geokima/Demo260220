using System;
using Cysharp.Threading.Tasks;

namespace Framework.Modules.Routine
{
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
}
