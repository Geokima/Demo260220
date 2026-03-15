using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.CardBattle
{
    public class DealDamageEffect : ICardEffect
    {
        public int BaseDamage { get; set; }

        public void Execute(CardBattleContext context)
        {
            var queue = CardBattleArchitecture.Instance.GetSystem<IActionQueueService>();

            foreach (var target in context.Targets)
            {
                // 替换掉 `new DamageAction` 垃圾回收污染，使用高效池化获取
                var action = ActionPool<DamageAction>.Allocate().Init(context.Source, target, BaseDamage);
                queue.Enqueue(action);
            }
        }
    }

    public class GainBlockEffect : ICardEffect
    {
        public int BaseBlock { get; set; }

        public void Execute(CardBattleContext context)
        {
            var queue = CardBattleArchitecture.Instance.GetSystem<IActionQueueService>();

            foreach (var target in context.Targets)
            {
                var action = ActionPool<BlockAction>.Allocate().Init(target, BaseBlock);
                queue.Enqueue(action);
            }
        }
    }

    public class ApplyBuffEffect : ICardEffect
    {
        public string BuffId { get; set; }
        public int Amount { get; set; }

        public void Execute(CardBattleContext context)
        {
            var queue = CardBattleArchitecture.Instance.GetSystem<IActionQueueService>();

            foreach (var target in context.Targets)
            {
                var action = ActionPool<ApplyBuffAction>.Allocate().Init(target, BuffId, Amount);
                queue.Enqueue(action);
            }
        }
    }
}
