using Framework;
using System;
using Framework.Modules.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Gameplay.Demo1.System
{
    public interface IGameRoundSystem : ISystem
    {
        int CurrentDay { get; }
        int CurrentRound { get; }
        int GoldPerRound { get; }
        
        void NextRound();
        void EndDay();
        void GrantRoundIncome();
        bool CheckVictory();
        bool CheckDefeat();
        bool CanContinue();
    }

    public class GameRoundSystem : AbstractSystem, IGameRoundSystem
    {
        private Demo1Model _model;

        public int CurrentDay => _model.Day.Value;
        public int CurrentRound => _model.Round.Value;
        public int GoldPerRound => _model.GoldPerRound.Value;

        public override void Init()
        {
            _model = this.GetModel<Demo1Model>();
        }

        public async void NextRound()
        {
            var uiSystem = this.GetSystem<IUISystem>();
            var blackScreen = OpenRoundTransition(uiSystem);
            await UniTask.Delay((int)(UI_BlackScreen.DefaultFadeDuration * 1000));

            AdvanceRound();
            this.GetSystem<IGameStateSystem>().SwitchTo(GameState.Selection);

            await UniTask.Delay(500);
            CloseRoundTransition(uiSystem, blackScreen);
        }

        private UI_BlackScreen OpenRoundTransition(IUISystem uiSystem)
        {
            uiSystem.Open<UI_BlackScreen>();
            var blackScreen = uiSystem.GetPanel<UI_BlackScreen>();
            if (_model.Round.Value == 6)
                blackScreen.SetColor(Color.white);
            return blackScreen;
        }

        private void CloseRoundTransition(IUISystem uiSystem, UI_BlackScreen blackScreen)
        {
            if (blackScreen != null)
                blackScreen.SetColor(Color.black);
            uiSystem.Close<UI_BlackScreen>();
        }

        private void AdvanceRound()
        {
            _model.Round.Value++;
            GrantRoundIncome();
            GrantRoundExp();
            RestoreRoundHp();
            TryLevelUp();

            if (_model.Round.Value > 6)
                EndDay();
        }

        private void GrantRoundExp()
        {
            _model.Exp.Value += 2;
        }

        private void RestoreRoundHp()
        {
            _model.CurrentHP.Value = _model.MaxHP.Value;
        }

        private void TryLevelUp()
        {
            while (_model.Exp.Value >= Demo1Const.ExpPerLevel)
            {
                _model.Exp.Value -= Demo1Const.ExpPerLevel;
                _model.Level.Value++;
                _model.MaxSlotCount.Value = Math.Min(Demo1Const.MaxSlots, _model.MaxSlotCount.Value + 2);
            }
        }

        public void EndDay()
        {
            _model.Day.Value++;
            _model.Round.Value = 1;
            _model.Progress.Value++;
        }

        public void GrantRoundIncome()
        {
            _model.Gold.Value += GoldPerRound;
        }

        public bool CheckVictory()
        {
            return _model.Progress.Value >= Demo1Const.MaxProgress;
        }

        public bool CheckDefeat()
        {
            return _model.CurrentHP.Value <= 0;
        }

        public bool CanContinue()
        {
            return !CheckVictory() && !CheckDefeat();
        }
    }
}
