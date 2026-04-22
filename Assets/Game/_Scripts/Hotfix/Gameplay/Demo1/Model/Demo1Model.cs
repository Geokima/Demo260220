using Framework;

namespace Game.Gameplay.Demo1
{
    public class Demo1Model : AbstractModel
    {
        #region 玩家属性
        public BindableProperty<int> CurrentHP = new BindableProperty<int>(100);
        public BindableProperty<int> MaxHP = new BindableProperty<int>(100);
        public BindableProperty<int> PlayerShield = new BindableProperty<int>(0);
        public BindableProperty<int> Prestige = new BindableProperty<int>(Demo1Const.MaxPrestige);
        public BindableProperty<int> PlayerPoison = new BindableProperty<int>(0);
        #endregion

        #region 敌人属性
        public BindableProperty<int> EnemyHP = new BindableProperty<int>(100);
        public BindableProperty<int> EnemyMaxHP = new BindableProperty<int>(100);
        public BindableProperty<int> EnemyShield = new BindableProperty<int>(0);
        public BindableProperty<int> EnemyPoison = new BindableProperty<int>(0);
        public BindableList<CardModel> EnemyCards = new BindableList<CardModel>();
        #endregion

        #region 经济系统
        public BindableProperty<int> Gold = new BindableProperty<int>(10);
        public BindableProperty<int> GoldPerRound = new BindableProperty<int>(Demo1Const.DefaultGoldPerRound);
        public BindableProperty<int> Level = new BindableProperty<int>(1);
        public BindableProperty<int> Exp = new BindableProperty<int>(0);
        public BindableProperty<int> MaxSlotCount = new BindableProperty<int>(Demo1Const.InitialSlots);
        #endregion

        #region 时间/回合
        public BindableProperty<int> Day = new BindableProperty<int>(1);
        public BindableProperty<int> Round = new BindableProperty<int>(1);
        public BindableProperty<int> Progress = new BindableProperty<int>(0);
        public BindableProperty<RoundPhase> CurrentRoundPhase = new BindableProperty<RoundPhase>(RoundPhase.Choose);
        #endregion

        #region 场景状态
        public BindableProperty<GameState> CurrentGameState = new BindableProperty<GameState>(GameState.Selection);
        public BindableProperty<int> VisualLockCount = new BindableProperty<int>(0);
        public BindableProperty<string> VisualLockReason = new BindableProperty<string>(string.Empty);
        #endregion

        #region 卡牌槽位
        public BindableList<CardModel> ActiveSlots = new BindableList<CardModel>();
        public BindableList<CardModel> BenchCards = new BindableList<CardModel>();
        public BindableList<CardModel> UpperSlots = new BindableList<CardModel>();
        public BindableList<string> Skills = new BindableList<string>();
        #endregion

        public override void Init()
        {
            CurrentHP.Value = 100;
            MaxHP.Value = 100;
            PlayerShield.Value = 0;
            Prestige.Value = Demo1Const.MaxPrestige;
            PlayerPoison.Value = 0;

            EnemyHP.Value = 100;
            EnemyMaxHP.Value = 100;
            EnemyShield.Value = 0;
            EnemyPoison.Value = 0;

            Gold.Value = 10;
            GoldPerRound.Value = Demo1Const.DefaultGoldPerRound;
            Level.Value = 1;
            Exp.Value = 0;
            MaxSlotCount.Value = Demo1Const.InitialSlots;

            Day.Value = 1;
            Round.Value = 1;
            Progress.Value = 0;
            CurrentRoundPhase.Value = RoundPhase.Choose;

            CurrentGameState.Value = GameState.None;
            VisualLockCount.Value = 0;
            VisualLockReason.Value = string.Empty;

            ActiveSlots.Clear();
            BenchCards.Clear();
            UpperSlots.Clear();
            Skills.Clear();
            EnemyCards.Clear();
        }
    }
}
