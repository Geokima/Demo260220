using Framework.Modules.Procedure;
using Game.Gameplay.Demo1.Event;
using Game.Gameplay.Demo1.System;

namespace Game.Gameplay.Demo1.Procedure
{
    public class SelectionProcedure : ProcedureBase
    {
        public override void OnEnter()
        {
            var model = Architecture.GetModel<Demo1Model>();
            model.CurrentSceneMode.Value = SceneMode.Selection;
            model.CurrentRoundPhase.Value = RoundPhase.Choose;

            Architecture.RegisterEvent<SelectSceneModeEvent>(OnSelectSceneMode);
        }

        private void OnSelectSceneMode(SelectSceneModeEvent e)
        {
            var model = Architecture.GetModel<Demo1Model>();
            model.CurrentSceneMode.Value = e.Mode;
            ChangeProcedure<EncounterSceneProcedure>();
        }

        private void UnregisterEvents()
        {
            Architecture.UnregisterEvent<SelectSceneModeEvent>(OnSelectSceneMode);
        }

        public override void OnExit()
        {
            UnregisterEvents();
        }
    }
}
