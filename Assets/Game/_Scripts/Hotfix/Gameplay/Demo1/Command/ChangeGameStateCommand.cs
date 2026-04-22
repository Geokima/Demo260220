using Framework;
using Game.Gameplay.Demo1.Event;
using Game.Gameplay.Demo1.System;

namespace Game.Gameplay.Demo1.Command
{
    public class ChangeGameStateCommand : AbstractCommand
    {
        public GameState State;
        public int Data;
        public override void Execute(object sender)
        {
            this.GetSystem<IGameStateSystem>().SwitchTo(State, Data);
        }
    }

    public class QuitEncounterCommand : AbstractCommand
    {
        public override void Execute(object sender)
        {
            var model = this.GetModel<Demo1Model>();
            this.GetSystem<IGameRoundSystem>().NextRound();
            this.GetSystem<IGameStateSystem>().SwitchTo(GameState.Selection);
        }
    }
}
