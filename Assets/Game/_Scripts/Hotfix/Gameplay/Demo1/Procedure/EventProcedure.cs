using Framework.Modules.Procedure;
using Game.Gameplay.Demo1.Event;

namespace Game.Gameplay.Demo1.Procedure
{
    public class EventProcedure : ProcedureBase
    {
        public override void OnEnter()
        {
            var model = Architecture.GetModel<Demo1Model>();
            model.CurrentRoundPhase.Value = RoundPhase.InEvent;

            Architecture.RegisterEvent<EventCompleteEvent>(OnEventComplete);
        }

        private void OnEventComplete(EventCompleteEvent e)
        {
            UnregisterEvents();
            Owner.ChangeProcedure<RewardProcedure>();
        }

        private void UnregisterEvents()
        {
            Architecture.UnregisterEvent<EventCompleteEvent>(OnEventComplete);
        }

        public override void OnExit()
        {
            UnregisterEvents();
        }
    }
}
