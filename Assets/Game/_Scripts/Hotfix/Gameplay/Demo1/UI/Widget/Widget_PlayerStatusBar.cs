using Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Demo1.UI.Widget
{
    public class Widget_PlayerStatusBar : MonoBehaviour
    {
        [Header("Prestige")]
        public Text PrestigeText;

        [Header("Gold")]
        public Text GoldText;
        public Text GoldPerRoundText;

        [Header("Level/Exp")]
        public Text LevelText;
        public Text ExpText;

        [Header("Day/Round")]
        public Text DayRoundText;

        [Header("Progress")]
        public Text ProgressText;

        [Header("Format Strings")]
        public string PrestigeFormat = "Prestige {0}";
        public string GoldFormat = "${0}";
        public string GoldPerRoundFormat = "(+{0})";
        public string LevelFormat = "Lv {0}";
        public string ExpFormat = "Exp {0}/{1}";
        public string DayRoundFormat = "Day {0} Round {1}/6";
        public string ProgressFormat = "Progress {0}/{1}";

        private Demo1Model _model;

        private IUnregister _prestigeUnregister;
        private IUnregister _goldUnregister;
        private IUnregister _goldPerRoundUnregister;
        private IUnregister _levelUnregister;
        private IUnregister _expUnregister;
        private IUnregister _dayUnregister;
        private IUnregister _roundUnregister;
        private IUnregister _progressUnregister;

        private void OnEnable()
        {
            Bind();
        }

        private void OnDisable()
        {
            Unbind();
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private void Bind()
        {
            if (_model != null)
                return;

            _model = Demo1Architecture.Instance.GetModel<Demo1Model>();
            if (_model == null)
                return;

            _prestigeUnregister = _model.Prestige.RegisterWithInitValue(_ => UpdatePrestige());
            _goldUnregister = _model.Gold.RegisterWithInitValue(_ => UpdateGold());
            _goldPerRoundUnregister = _model.GoldPerRound.RegisterWithInitValue(_ => UpdateGold());
            _levelUnregister = _model.Level.RegisterWithInitValue(_ => UpdateLevelExp());
            _expUnregister = _model.Exp.RegisterWithInitValue(_ => UpdateLevelExp());
            _dayUnregister = _model.Day.RegisterWithInitValue(_ => UpdateDayRound());
            _roundUnregister = _model.Round.RegisterWithInitValue(_ => UpdateDayRound());
            _progressUnregister = _model.Progress.RegisterWithInitValue(_ => UpdateProgress());
        }

        private void Unbind()
        {
            _prestigeUnregister?.Unregister();
            _goldUnregister?.Unregister();
            _goldPerRoundUnregister?.Unregister();
            _levelUnregister?.Unregister();
            _expUnregister?.Unregister();
            _dayUnregister?.Unregister();
            _roundUnregister?.Unregister();
            _progressUnregister?.Unregister();
        }

        private void UpdatePrestige()
        {
            if (PrestigeText != null)
                PrestigeText.text = string.Format(PrestigeFormat, _model.Prestige.Value);
        }

        private void UpdateGold()
        {
            if (GoldText != null)
                GoldText.text = string.Format(GoldFormat, _model.Gold.Value);
            if (GoldPerRoundText != null)
                GoldPerRoundText.text = string.Format(GoldPerRoundFormat, _model.GoldPerRound.Value);
        }

        private void UpdateLevelExp()
        {
            if (LevelText != null)
                LevelText.text = string.Format(LevelFormat, _model.Level.Value);
            if (ExpText != null)
                ExpText.text = string.Format(ExpFormat, _model.Exp.Value, Demo1Const.ExpPerLevel);
        }

        private void UpdateDayRound()
        {
            if (DayRoundText != null)
                DayRoundText.text = string.Format(DayRoundFormat, _model.Day.Value, _model.Round.Value);
        }

        private void UpdateProgress()
        {
            if (ProgressText != null)
                ProgressText.text = string.Format(ProgressFormat, _model.Progress.Value, Demo1Const.MaxProgress);
        }
    }
}
