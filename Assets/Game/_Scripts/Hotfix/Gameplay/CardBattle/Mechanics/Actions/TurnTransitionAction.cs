using Cysharp.Threading.Tasks;
using Framework;
using UnityEngine;

namespace Game.Gameplay.CardBattle
{
    /// <summary>
    /// 回合切换动作。用于在逻辑上切断同步递归，并提供表现层展示“轮到谁了”的时机。
    /// </summary>
    public class TurnTransitionAction : IPoolableAction
    {
        public IActionQueueService Superior { get; set; }
        private EntityType _nextTurn;

        public TurnTransitionAction Init(EntityType nextTurn)
        {
            _nextTurn = nextTurn;
            return this;
        }

        public void Reset()
        {
            Superior = null;
        }

        public async UniTask ExecuteAsync()
        {
            var model = Superior.GetModel<BattleModel>();
            
            // 发送切换事件给表现层 (显示 "Player Turn" / "Enemy Turn" 大字)
            Superior.SendEvent(new TurnTransitionVisualEvent { NextTurn = _nextTurn });
            
            // 等待表现层锁定结束
            await UniTask.WaitUntil(() => model.VisualLockCount.Value == 0);

            // 真正切换逻辑
            var turnService = Superior.GetSystem<ITurnService>();
            if (_nextTurn == EntityType.Player)
            {
                turnService.StartPlayerTurn();
            }
            else
            {
                turnService.ProcessEnemyTurn();
            }
        }

        public void Recycle() => ActionPool<TurnTransitionAction>.Recycle(this);
    }

    public struct TurnTransitionVisualEvent
    {
        public EntityType NextTurn;
    }
}
