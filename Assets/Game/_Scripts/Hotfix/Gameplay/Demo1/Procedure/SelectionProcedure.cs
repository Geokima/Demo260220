using Framework.Modules.Procedure;
using Game.Gameplay.Demo1.Event;

namespace Game.Gameplay.Demo1.Procedure
{
    public class SelectionProcedure : ProcedureBase
    {
        public override void OnEnter()
        {
            var model = Architecture.GetModel<Demo1Model>();
            model.CurrentSceneMode.Value = SceneMode.Selection;
            model.CurrentRoundPhase.Value = RoundPhase.Choose;

            if (model.Round.Value == 3 || model.Round.Value == 6)
            {
                Architecture.RegisterEvent<EventCompleteEvent>(OnEventComplete);
            }
            else
            {
                Architecture.RegisterEvent<SelectShopEvent>(OnSelectShop);
                Architecture.RegisterEvent<SelectWorkEvent>(OnSelectWork);
                Architecture.RegisterEvent<SelectTreasureEvent>(OnSelectTreasure);
            }
        }

        private void OnEventComplete(EventCompleteEvent e)
        {
            UnregisterEvents();
            Owner.ChangeProcedure<RewardProcedure>();
        }

        private void OnSelectShop(SelectShopEvent e)
        {
            UnregisterEvents();
            Owner.ChangeProcedure<EventProcedure>();
        }

        private void OnSelectWork(SelectWorkEvent e)
        {
            UnregisterEvents();
            Owner.ChangeProcedure<EventProcedure>();
        }

        private void OnSelectTreasure(SelectTreasureEvent e)
        {
            UnregisterEvents();
            Owner.ChangeProcedure<EventProcedure>();
        }

        private void UnregisterEvents()
        {
            Architecture.UnregisterEvent<SelectShopEvent>(OnSelectShop);
            Architecture.UnregisterEvent<SelectWorkEvent>(OnSelectWork);
            Architecture.UnregisterEvent<SelectTreasureEvent>(OnSelectTreasure);
            Architecture.UnregisterEvent<EventCompleteEvent>(OnEventComplete);
        }

        public override void OnExit()
        {
            UnregisterEvents();
        }
    }
}
