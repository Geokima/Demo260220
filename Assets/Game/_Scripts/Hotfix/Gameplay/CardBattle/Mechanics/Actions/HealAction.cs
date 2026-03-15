using Cysharp.Threading.Tasks;
using UnityEngine;
using Framework;

namespace Game.Gameplay.CardBattle
{
    public class HealAction : IPoolableAction
    {
        public bool IsCompleted { get; private set; } = false;
        public IActionQueueService Superior { get; set; }

        private EntityData _target;
        private int _healAmount;

        public HealAction Init(EntityData target, int healAmount)
        {
            _target = target;
            _healAmount = healAmount;
            return this;
        }

        public void Reset()
        {
            _target = null;
            _healAmount = 0;
            IsCompleted = false;
            Superior = null;
        }

        public async UniTask ExecuteAsync()
        {
            int actualHeal = Mathf.Min(_healAmount, _target.MaxHp.Value - _target.CurrentHp.Value);
            
            if (actualHeal > 0)
            {
                // 1. 发送表现事件
                Superior.SendEvent(new DamageVisualEvent 
                { 
                    Sender = this, 
                    Target = _target, 
                    Amount = -actualHeal // 负数代表治疗视觉
                });

                // 2. 动画锁定
                var model = Superior.GetModel<BattleModel>();
                await UniTask.WaitUntil(() => model.VisualLockCount.Value == 0);

                // 3. 数值应用
                _target.CurrentHp.Value += actualHeal;
            }

            IsCompleted = true;
        }

        public void Recycle() => ActionPool<HealAction>.Recycle(this);
    }
}
