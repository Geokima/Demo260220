using Framework;
using Framework.Modules.Procedure;
using Game.Gameplay.Demo1.Procedure;
using Game.Procedures;
using UnityEngine;

namespace Game.Gameplay.Demo1
{
    public class Demo1ArchitectureRunner : MonoBehaviour
    {
        [SerializeField]private bool IsDebug = true;

        public void Shutdown()
        {
            GameArchitecture.Instance.GetSystem<IProcedureSystem>().ChangeProcedure<MainProcedure>();
        }

        private void Awake()
        {
            Demo1Architecture.Launch();

            var procedureSystem = Demo1Architecture.Instance.GetSystem<IProcedureSystem>();

            procedureSystem.RegisterProcedure(new InitProcedure());
            procedureSystem.RegisterProcedure(new SelectionProcedure());
            procedureSystem.RegisterProcedure(new EncounterSceneProcedure());
            procedureSystem.RegisterProcedure(new RewardProcedure());
            procedureSystem.RegisterProcedure(new GameOverProcedure());

            procedureSystem.Start<InitProcedure>();
        }

        private void Update()
        {
            Demo1Architecture.Instance.Update();
        }

        private void FixedUpdate()
        {
            Demo1Architecture.Instance.FixedUpdate();
        }

        private void OnDestroy()
        {
            Demo1Architecture.Instance.Shutdown();
        }

        private void OnGUI()
        {
            if (!IsDebug)
                return;

            float x = 10;
            float y = 10;
            float width = 300;
            float lineHeight = 25;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperLeft
            };

            var model = Demo1Architecture.Instance.GetModel<Demo1Model>();
            var procedureSystem = Demo1Architecture.Instance.GetSystem<IProcedureSystem>();

            GUI.Label(new Rect(x, y, width, lineHeight), $"Procedure: {procedureSystem.CurrentProcedure?.Name ?? "None"}", style);
            y += lineHeight;
            GUI.Label(new Rect(x, y, width, lineHeight), $"SceneMode: {model.CurrentSceneMode}", style);
            y += lineHeight;
            GUI.Label(new Rect(x, y, width, lineHeight), $"Round: Day {model.Day} Round {model.Round}/6", style);
            y += lineHeight;
            GUI.Label(new Rect(x, y, width, lineHeight), $"HP: {model.CurrentHP}/{model.MaxHP}", style);
            y += lineHeight;
            GUI.Label(new Rect(x, y, width, lineHeight), $"Gold: {model.Gold} (+{model.GoldPerRound})", style);
            y += lineHeight;
            GUI.Label(new Rect(x, y, width, lineHeight), $"Prestige: {model.Prestige}", style);
            y += lineHeight;
            GUI.Label(new Rect(x, y, width, lineHeight), $"Level: Lv{model.Level} Exp {model.Exp}/{Demo1Const.ExpPerLevel}", style);
            y += lineHeight;
            GUI.Label(new Rect(x, y, width, lineHeight), $"Slots: {model.MaxSlotCount}", style);
            y += lineHeight;
            GUI.Label(new Rect(x, y, width, lineHeight), $"Progress: {model.Progress}/{Demo1Const.MaxProgress}", style);
            y += lineHeight;
            GUI.Label(new Rect(x, y, width, lineHeight), $"Phase: {model.CurrentRoundPhase}", style);
        }
    }
}
