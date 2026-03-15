using Framework;

namespace Game.Gameplay.CardBattle
{
    /// <summary>
    /// 卡牌战斗独立子架构
    /// 包含对局内所有的状态、队列与数值管线
    /// 战斗结束时应调用 Shutdown 销毁
    /// </summary>
    public class CardBattleArchitecture : Architecture<CardBattleArchitecture>
    {
        protected override void RegisterModule()
        {
            // Models
            this.RegisterModel(new BattleModel());
            
            // Services
            this.RegisterSystem<IActionQueueService>(new ActionQueueService());
            this.RegisterSystem<IBuffService>(new BuffService());
            this.RegisterSystem<INumericalService>(new NumericalService());
            this.RegisterSystem<ITurnService>(new TurnService());
            this.RegisterSystem<IRelicService>(new RelicService()); // 统一管家
        }
    }
}
