using Cysharp.Threading.Tasks;
using Framework;

namespace Game.Gameplay.CardBattle
{
    public class BlockAction : IPoolableAction
    {
        public bool IsCompleted { get; private set; } = false;
        public IActionQueueService Superior { get; set; }
        private INumericalService Numerical => Superior.GetSystem<INumericalService>();

        private EntityData _target;
        private int _amount;

        public BlockAction Init(EntityData target, int amount)
        {
            _target = target;
            _amount = amount;
            return this;
        }

        public void Reset()
        {
            _target = null;
            _amount = 0;
            IsCompleted = false;
            Superior = null;
        }

        public async UniTask ExecuteAsync()
        {
            int finalAmount = Numerical.CalculateBlock(_target, _amount);

            // 1. 发送表现层事件
            Superior.SendEvent(new BlockVisualEvent 
            { 
                Sender = this, 
                Target = _target, 
                Amount = finalAmount 
            });

            // 2. 等待表现层锁定 (动画播放)
            var model = Superior.GetModel<BattleModel>();
            await UniTask.WaitUntil(() => model.VisualLockCount.Value == 0);

            // 3. 真正产生数值跳变
            _target.Block.Value += finalAmount;

            IsCompleted = true;
        }

        public void Recycle() => ActionPool<BlockAction>.Recycle(this);
    }
}
