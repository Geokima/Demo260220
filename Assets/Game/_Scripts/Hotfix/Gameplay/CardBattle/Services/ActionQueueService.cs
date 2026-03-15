using System.Collections.Generic;
using Framework;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Gameplay.CardBattle
{
    public interface IActionQueueService : ISystem
    {
        void Enqueue(IBattleAction action);
        UniTask ProcessQueueAsync();
        void Clear();
    }

    /// <summary> 高效 Action 执行管线 </summary>
    public class ActionQueueService : AbstractSystem, IActionQueueService
    {
        private readonly Queue<IBattleAction> _queue = new Queue<IBattleAction>();
        private bool _isProcessing = false;

        public override void Init() { }

        public void Enqueue(IBattleAction action)
        {
            if (action == null) return;
            UnityEngine.Debug.Log($"[ActionQueue] Enqueue: {action.GetType().Name}");
            action.Superior = this;
            _queue.Enqueue(action);

            // 【关键修复】入队时自动触发处理循环，不必依赖外部轮询
            ProcessQueueAsync().Forget();
        }

        public async UniTask ProcessQueueAsync()
        {
            if (_isProcessing) return;
            _isProcessing = true;

            while (_queue.Count > 0)
            {
                var action = _queue.Dequeue();
                UnityEngine.Debug.Log($"[ActionQueue] Executing: {action.GetType().Name}");
                try
                {
                    await action.ExecuteAsync();
                }
                finally
                {
                    // 无论成功失败都必须回收
                    action.Recycle();
                }
            }

            _isProcessing = false;
        }

        /// <summary> 强行清理整条管线中的残留 Action（用于退出战斗） </summary>
        public void Clear()
        {
            Debug.Log($"[ActionQueue] 强制清理? {_queue.Count} 个残留 Action 并启动回收...");
            while (_queue.Count > 0)
            {
                var action = _queue.Dequeue();
                action.Recycle();
            }
            _isProcessing = false;
        }
    }
}
