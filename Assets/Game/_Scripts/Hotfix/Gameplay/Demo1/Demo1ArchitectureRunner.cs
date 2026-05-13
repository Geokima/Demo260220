using Framework;
using Framework.Modules.Procedure;
using Framework.Modules.UI;
using Game.Gameplay.Demo1.System;
using Game.Procedures;
using UnityEngine;

namespace Game.Gameplay.Demo1
{
    public class Demo1ArchitectureRunner : MonoBehaviour
    {
        [SerializeField] private bool IsDebug = true;

        private IBattleSystem _battleSystem;

        public void Quit()
        {
            GameArchitecture.Instance.GetSystem<IProcedureSystem>().ChangeProcedure<MainProcedure>();
        }

        private void Awake()
        {
            Debug.Log("[Demo1ArchitectureRunner] Launch Demo1Architecture...");
            Demo1Architecture.Launch();
            _battleSystem = Demo1Architecture.Instance.GetSystem<IBattleSystem>();
            Demo1Architecture.Instance.GetSystem<IUISystem>().Open<UI_Demo1PlayerPanel>();
        }

        private void Update()
        {
            Demo1Architecture.Instance.Update();

            if (_battleSystem.IsInBattle)
            {
                _battleSystem.UpdateBattle(UnityEngine.Time.deltaTime);
            }
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

            GUI.Label(new Rect(x, y, width, lineHeight), $"GameState: {model.CurrentGameState}", style);
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
        }
    }
}
