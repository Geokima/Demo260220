using System.Collections.Generic;
using Framework;
using Cysharp.Threading.Tasks;

namespace Game.Gameplay.CardBattle
{
    public class PlayCardWrapperAction : IPoolableAction
    {
        public bool IsCompleted { get; private set; } = false;
        public IActionQueueService Superior { get; set; }

        private CardData _card;
        private EntityData _target;

        public PlayCardWrapperAction Init(CardData card, EntityData target)
        {
            _card = card;
            _target = target;
            IsCompleted = false;
            return this;
        }

        public void Reset()
        {
            _card = null;
            _target = null;
            IsCompleted = false;
            Superior = null;
        }

        public async UniTask ExecuteAsync()
        {
            var model = Superior.GetModel<BattleModel>();

            if (model.Player.Energy.Value < _card.CurrentCost.Value)
            {
                UnityEngine.Debug.LogWarning("[Logic] 能量不足");
                IsCompleted = true;
                return;
            }

            model.Player.Energy.Value -= _card.CurrentCost.Value;
            model.Hand.Remove(_card);
            model.DiscardPile.Add(_card);

            var targets = new List<EntityData>();
            if (_card.TargetType == CardTargetType.SingleEnemy && _target != null)
                targets.Add(_target);
            else if (_card.TargetType == CardTargetType.Self)
                targets.Add(model.Player);
            else if (_card.TargetType == CardTargetType.AllEnemies)
                targets.AddRange(model.Enemies);

            var context = new CardBattleContext 
            { 
                Source = model.Player, 
                Targets = targets, 
                Card = _card 
            };

            foreach (var effect in _card.OnPlayEffects)
                effect.Execute(context);

            await UniTask.WaitUntil(() => model.VisualLockCount.Value == 0);
            IsCompleted = true;
        }

        public void Recycle() => ActionPool<PlayCardWrapperAction>.Recycle(this);
    }
}
