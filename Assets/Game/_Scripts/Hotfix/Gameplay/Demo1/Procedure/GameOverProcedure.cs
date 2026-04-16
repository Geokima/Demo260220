using Framework.Modules.Procedure;

namespace Game.Gameplay.Demo1.Procedure
{
    public class GameOverProcedure : ProcedureBase
    {
        public override void OnEnter()
        {
            var model = Architecture.GetModel<Demo1Model>();
            model.CurrentSceneMode.Value = SceneMode.GameOver;
            model.CurrentRoundPhase.Value = RoundPhase.End;
        }
    }
}
