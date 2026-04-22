using Framework;
using Framework.Modules.Config;
using Framework.Modules.Res;
using Framework.Modules.Timer;
using Framework.Modules.UI;
using Game.Gameplay.Demo1.System;

namespace Game.Gameplay.Demo1
{
    public class Demo1Architecture : Architecture<Demo1Architecture>
    {
        protected override void RegisterModule()
        {
            RegisterSystem(GameArchitecture.Instance.GetSystem<IConfigSystem>());
            RegisterSystem(GameArchitecture.Instance.GetSystem<IResSystem>());

            RegisterSystem<ITimerSystem>(new TimerSystem());
            RegisterSystem<IUISystem>(new UISystem());
            RegisterSystem<IGameStateSystem>(new GameStateSystem());
            RegisterSystem<IDragSystem>(new DragSystem());
            RegisterSystem<ISelectionOptionSystem>(new SelectionOptionSystem());
            RegisterSystem<IShopPurchaseSystem>(new ShopPurchaseSystem());
            RegisterSystem<IGameRoundSystem>(new GameRoundSystem());
            RegisterSystem<IBattleSystem>(new BattleSystem());

            RegisterModel(new Demo1Model());
        }
    }
}
