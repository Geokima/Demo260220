using Framework;

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

        public void NextRound()
        {
            _model.Round.Value++;
            GrantRoundIncome();

            if (_model.Round.Value > Demo1Const.MaxProgress)
            {
                EndDay();
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
