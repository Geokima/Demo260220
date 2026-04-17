using Framework.Modules.Procedure;
using Game.Gameplay.Demo1.Event;

namespace Game.Gameplay.Demo1.Procedure
{
    public class EncounterSceneProcedure : ProcedureBase
    {
        public override void OnEnter()
        {
            var model = Architecture.GetModel<Demo1Model>();
            model.CurrentRoundPhase.Value = RoundPhase.InEvent;

            Architecture.RegisterEvent<QuitSceneEvent>(OnQuitEncounter);
        }

        private void OnQuitEncounter(QuitSceneEvent e)
        {
            ChangeProcedure<RewardProcedure>();
        }

        public override void OnExit()
        {
            Architecture.UnregisterEvent<QuitSceneEvent>(OnQuitEncounter);
        }
    }
}
