using Framework;
using Framework.Modules.Procedure;
using Framework.Modules.Timer;
using Game.Gameplay.Demo1.UI;

namespace Game.Gameplay.Demo1
{
    public class Demo1Architecture : Architecture<Demo1Architecture>
    {
        protected override void RegisterModule()
        {
            RegisterModel(new Demo1Model());
            RegisterSystem<ITimerSystem>(new TimerSystem());
            RegisterSystem<IProcedureSystem>(new ProcedureSystem());
            RegisterSystem<IScenePanelSystem>(new ScenePanelSystem());
        }
    }
}
