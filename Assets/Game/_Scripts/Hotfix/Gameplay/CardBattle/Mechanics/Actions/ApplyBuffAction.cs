using Framework;
using Cysharp.Threading.Tasks;
using System.Linq;

namespace Game.Gameplay.CardBattle
{
    public class ApplyBuffAction : IPoolableAction
    {
        public bool IsCompleted { get; private set; } = false;
        public IActionQueueService Superior { get; set; }

        private EntityData _target;
        private string _buffId;
        private int _amount;

        public ApplyBuffAction Init(EntityData target, string buffId, int amount)
        {
            _target = target;
            _buffId = buffId;
            _amount = amount;
            return this;
        }

        public void Reset()
        {
            _target = null;
            _buffId = null;
            _amount = 0;
            IsCompleted = false;
            Superior = null;
        }

        public async UniTask ExecuteAsync()
        {
            // 1. 发送表现表现
            Superior.SendEvent(new BuffVisualEvent 
            { 
                Sender = this, 
                Target = _target, 
                BuffId = _buffId, 
                Value = _amount 
            });

            // 2. 动画锁定
            var model = Superior.GetModel<BattleModel>();
            await UniTask.WaitUntil(() => model.VisualLockCount.Value == 0);

            // 3. 应用数据
            var existing = _target.Buffs.FirstOrDefault(b => b.Id == _buffId);
            if (existing != null)
                existing.Value += _amount;
            else
                _target.Buffs.Add(new Shared.DTOs.CardBattle.BuffDto { Id = _buffId, Value = _amount });

            IsCompleted = true;
        }

        public void Recycle() => ActionPool<ApplyBuffAction>.Recycle(this);
    }
}
