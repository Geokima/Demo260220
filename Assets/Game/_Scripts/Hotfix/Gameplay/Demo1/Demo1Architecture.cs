using Framework;
using Framework.Modules.Procedure;
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
            // 注册模型
            RegisterModel(new Demo1Model());
            // 注册系统
            RegisterSystem<IResSystem>(new ResSystem());
            RegisterSystem<ITimerSystem>(new TimerSystem());
            RegisterSystem<IProcedureSystem>(new ProcedureSystem());
            RegisterSystem<IUISystem>(new UISystem());
            RegisterSystem<ISceneModeSystem>(new SceneModeSystem());
            RegisterSystem<IDragSystem>(new DragSystem());
            RegisterSystem<ISelectionOptionSystem>(new SelectionOptionSystem());
        }
    }
}
