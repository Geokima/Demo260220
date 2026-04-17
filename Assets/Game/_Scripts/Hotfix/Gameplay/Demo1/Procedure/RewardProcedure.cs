using Framework.Modules.Procedure;
using Game.Gameplay.Demo1.Event;

namespace Game.Gameplay.Demo1.Procedure
{
    public class RewardProcedure : ProcedureBase
    {
        public override void OnEnter()
        {
            var model = Architecture.GetModel<Demo1Model>();
            model.CurrentSceneMode.Value = SceneMode.Reward;
            model.CurrentRoundPhase.Value = RoundPhase.Complete;

            Architecture.RegisterEvent<CollectRewardEvent>(OnCollected);
        }

        private void OnCollected(CollectRewardEvent e)
        {
            var model = Architecture.GetModel<Demo1Model>();

            model.Round.Value++;
            if (model.Round.Value > 6)
            {
                model.Round.Value = 1;
                model.Day.Value++;
            }

            if (model.Prestige.Value <= 0)
            {
                ChangeProcedure<GameOverProcedure>();
                return;
            }

            if (model.Progress.Value >= Demo1Const.MaxProgress)
            {
                ChangeProcedure<GameOverProcedure>();
                return;
            }

            ChangeProcedure<SelectionProcedure>();
        }


        public override void OnExit()
        {
            Architecture.UnregisterEvent<CollectRewardEvent>(OnCollected);
        }
    }
}
