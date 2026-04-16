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
            UnregisterEvents();

            var model = Architecture.GetModel<Demo1Model>();

            model.Round.Value++;
            if (model.Round.Value > 6)
            {
                model.Round.Value = 1;
                model.Day.Value++;
            }

            if (model.Prestige.Value <= 0)
            {
                Owner.ChangeProcedure<GameOverProcedure>();
                return;
            }

            if (model.Progress.Value >= Demo1Const.MaxProgress)
            {
                Owner.ChangeProcedure<GameOverProcedure>();
                return;
            }

            Owner.ChangeProcedure<SelectionProcedure>();
        }

        private void UnregisterEvents()
        {
            Architecture.UnregisterEvent<CollectRewardEvent>(OnCollected);
        }

        public override void OnExit()
        {
            UnregisterEvents();
        }
    }
}
