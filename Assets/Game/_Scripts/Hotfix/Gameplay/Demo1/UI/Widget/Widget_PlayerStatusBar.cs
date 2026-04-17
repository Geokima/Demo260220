using Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Gameplay.Demo1.UI.Widget
{
    public class Widget_PlayerStatusBar : MonoBehaviour
    {
        [Header("HP")]
        public Image HpFillImage;
        public Text HpText;

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
        public string HpFormat = "{0}/{1}";
        public string PrestigeFormat = "Prestige {0}";
        public string GoldFormat = "${0}";
        public string GoldPerRoundFormat = "(+{0})";
        public string LevelFormat = "Lv {0}";
        public string ExpFormat = "Exp {0}/{1}";
        public string DayRoundFormat = "Day {0} Round {1}/6";
        public string ProgressFormat = "Progress {0}/{1}";

        private Demo1Model _model;

        private IUnregister _hpUnregister;
        private IUnregister _maxHpUnregister;
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

            _hpUnregister = _model.CurrentHP.RegisterWithInitValue(_ => UpdateHp());
            _maxHpUnregister = _model.MaxHP.RegisterWithInitValue(_ => UpdateHp());
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
            _hpUnregister?.Unregister();
            _maxHpUnregister?.Unregister();
            _prestigeUnregister?.Unregister();
            _goldUnregister?.Unregister();
            _goldPerRoundUnregister?.Unregister();
            _levelUnregister?.Unregister();
            _expUnregister?.Unregister();
            _dayUnregister?.Unregister();
            _roundUnregister?.Unregister();
            _progressUnregister?.Unregister();

            _hpUnregister = null;
            _maxHpUnregister = null;
            _prestigeUnregister = null;
            _goldUnregister = null;
            _goldPerRoundUnregister = null;
            _levelUnregister = null;
            _expUnregister = null;
            _dayUnregister = null;
            _roundUnregister = null;
            _progressUnregister = null;

            _model = null;
        }

        private void UpdateHp()
        {
            if (_model == null)
                return;

            int current = _model.CurrentHP.Value;
            int max = Mathf.Max(1, _model.MaxHP.Value);

            if (HpText != null)
                HpText.text = string.Format(HpFormat, current, max);

            if (HpFillImage != null)
                HpFillImage.fillAmount = Mathf.Clamp01(current / (float)max);
        }

        private void UpdatePrestige()
        {
            if (_model == null)
                return;

            if (PrestigeText != null)
                PrestigeText.text = string.Format(PrestigeFormat, _model.Prestige.Value);
        }

        private void UpdateGold()
        {
            if (_model == null)
                return;

            if (GoldText != null)
                GoldText.text = string.Format(GoldFormat, _model.Gold.Value);

            if (GoldPerRoundText != null)
                GoldPerRoundText.text = string.Format(GoldPerRoundFormat, _model.GoldPerRound.Value);
        }

        private void UpdateLevelExp()
        {
            if (_model == null)
                return;

            if (LevelText != null)
                LevelText.text = string.Format(LevelFormat, _model.Level.Value);

            if (ExpText != null)
                ExpText.text = string.Format(ExpFormat, _model.Exp.Value, Demo1Const.ExpPerLevel);
        }

        private void UpdateDayRound()
        {
            if (_model == null)
                return;

            if (DayRoundText != null)
                DayRoundText.text = string.Format(DayRoundFormat, _model.Day.Value, _model.Round.Value);
        }

        private void UpdateProgress()
        {
            if (_model == null)
                return;

            int current = _model.Progress.Value;
            int max = Mathf.Max(1, Demo1Const.MaxProgress);

            if (ProgressText != null)
                ProgressText.text = string.Format(ProgressFormat, current, max);
        }
    }
}
