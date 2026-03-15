using Cysharp.Threading.Tasks;
using Framework;
using UnityEngine;

namespace Game.Gameplay.CardBattle
{
    public class DamageAction : IPoolableAction
    {
        public bool IsCompleted { get; private set; } = false;
        public IActionQueueService Superior { get; set; }
        private INumericalService Numerical => Superior.GetSystem<INumericalService>();

        private EntityData _source;
        private EntityData _target;
        private int _amount;

        public DamageAction Init(EntityData source, EntityData target, int amount)
        {
            _source = source;
            _target = target;
            _amount = amount;
            return this;
        }

        public DamageAction Init(EntityData target, int amount) => Init(null, target, amount);

        public void Reset()
        {
            _source = null;
            _target = null;
            _amount = 0;
            IsCompleted = false;
            Superior = null;
        }

        public async UniTask ExecuteAsync()
        {
            int finalAmount = Numerical.CalculateDamage(_source, _target, _amount);
            
            // 1. 发送表现层事件 (先于数值扣除)
            Superior.SendEvent(new DamageVisualEvent { Sender = this, Source = _source, Target = _target, Amount = finalAmount });

            // 2. 这里的等待是为了让表现层锁定 (例如播放受击动画)
            var model = Superior.GetModel<BattleModel>();
            await UniTask.WaitUntil(() => model.VisualLockCount.Value == 0);

            // 3. 真正执行数值扣除 (动画到达或播完后数值才跳变)
            int remainingDamage = finalAmount;

            // 护甲扣除
            if (_target.Block.Value > 0)
            {
                if (_target.Block.Value >= remainingDamage)
                {
                    _target.Block.Value -= remainingDamage;
                    remainingDamage = 0;
                }
                else
                {
                    remainingDamage -= _target.Block.Value;
                    _target.Block.Value = 0;
                }
            }

            // 血量扣除
            if (remainingDamage > 0)
            {
                _target.CurrentHp.Value -= remainingDamage;
            }

            // 4. 死亡判定
            if (_target.CurrentHp.Value <= 0)
            {
                // TODO: 可以在这里 Enqueue 一个 DieAction，或者直接触发死亡逻辑
                Debug.Log($"[Battle] {_target.Name} 已死亡");
            }

            IsCompleted = true;
        }

        public void Recycle() => ActionPool<DamageAction>.Recycle(this);
    }
}
