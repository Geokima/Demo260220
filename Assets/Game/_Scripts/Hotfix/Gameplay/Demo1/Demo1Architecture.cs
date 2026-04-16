using Framework;
using Framework.Modules.Procedure;
using Framework.Modules.Timer;
using Game.Gameplay.Demo1.Procedure;

namespace Game.Gameplay.Demo1
{
    public class Demo1Architecture : Architecture<Demo1Architecture>
    {
        protected override void RegisterModule()
        {
            this.RegisterModel(new Demo1Model());
            this.RegisterSystem<ITimerSystem>(new TimerSystem());

            var procedureSystem = new ProcedureSystem();
            this.RegisterSystem<IProcedureSystem>(procedureSystem);

            procedureSystem.RegisterProcedure(new SelectionProcedure());
            procedureSystem.RegisterProcedure(new EventProcedure());
            procedureSystem.RegisterProcedure(new RewardProcedure());
            procedureSystem.RegisterProcedure(new GameOverProcedure());

            procedureSystem.Start<SelectionProcedure>();
        }
    }
}
