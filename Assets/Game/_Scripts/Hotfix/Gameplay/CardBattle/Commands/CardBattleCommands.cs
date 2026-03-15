using Framework;
using UnityEngine;

namespace Game.Gameplay.CardBattle
{
    public class StartBattleCommand : AbstractCommand
    {
        public override void Execute(object sender)
        {
            this.GetSystem<ITurnService>().StartBattle();
        }
    }

    public class PlayCardCommand : AbstractCommand
    {
        public CardData Card { get; set; }
        public EntityData Target { get; set; }

        public override void Execute(object sender)
        {
            var queue = this.GetSystem<IActionQueueService>();
            
            // 纯净的传话筒：只需让架构产生一条 PlayCard 打出的动作丢入总队列去锁血执行
            var action = ActionPool<PlayCardWrapperAction>.Allocate().Init(Card, Target);
            queue.Enqueue(action);
        }
    }

    public class EndTurnCommand : AbstractCommand
    {
        public override void Execute(object sender)
        {
            this.GetSystem<ITurnService>().EndPlayerTurn();
        }
    }
}
