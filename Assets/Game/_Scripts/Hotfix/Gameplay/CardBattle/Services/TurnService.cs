using Framework;
using System;
using System.Collections.Generic;

namespace Game.Gameplay.CardBattle
{
    public interface ITurnService : ISystem
    {
        void StartBattle();
        void StartPlayerTurn();
        void EndPlayerTurn();
        void ProcessEnemyTurn();
    }

    public class TurnService : AbstractSystem, ITurnService
    {
        private BattleModel _model;

        public override void Init()
        {
            _model = this.GetModel<BattleModel>();
        }

        public void StartBattle()
        {
            UnityEngine.Debug.Log("[TurnService] StartBattle called");
            _model.TurnCount.Value = 0;
            StartPlayerTurn();
        }

        public void StartPlayerTurn()
        {
            UnityEngine.Debug.Log($"[TurnService] StartPlayerTurn: Turn {_model.TurnCount.Value + 1}");
            _model.TurnCount.Value++;
            
            // 抛出 TurnStartEvent 事件
            this.SendEvent(new TurnStartEvent { CurrentTurn = EntityType.Player, TurnCount = _model.TurnCount.Value });

            // 重置玩家资源
            _model.Player.Energy.Value = 3;
            _model.Player.Block.Value = 0;

            var queue = this.GetSystem<IActionQueueService>();

            // 压入系统级抽牌
            queue.Enqueue(ActionPool<DrawCardAction>.Allocate().Init(5));
        }

        public void EndPlayerTurn()
        {
            this.SendEvent(new TurnEndEvent { CurrentTurn = EntityType.Player });

            var queue = this.GetSystem<IActionQueueService>();
            var buffService = this.GetSystem<IBuffService>();
            
            // 处理玩家 Buff 衰减
            buffService.ProcessTurnEnd(_model.Player);

            // 丢弃手牌
            queue.Enqueue(ActionPool<DiscardHandAction>.Allocate());
            
            // 压入切换到敌人回合的动作
            queue.Enqueue(ActionPool<TurnTransitionAction>.Allocate().Init(EntityType.Enemy));
        }

        public void ProcessEnemyTurn()
        {
            var queue = this.GetSystem<IActionQueueService>();
            var buffService = this.GetSystem<IBuffService>();

            foreach (var enemy in _model.Enemies)
            {
                if (enemy.CurrentHp.Value <= 0) continue;

                enemy.Block.Value = 0;
                
                // 处理怪物 Buff 衰减
                buffService.ProcessTurnEnd(enemy);
                
                // 暂时硬编码怪物攻击
                queue.Enqueue(ActionPool<DamageAction>.Allocate().Init(enemy, _model.Player, 10));
            }

            // 敌人打完后，压入切换到玩家回合的动作
            queue.Enqueue(ActionPool<TurnTransitionAction>.Allocate().Init(EntityType.Player));
        }
    }
}
