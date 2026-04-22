namespace Game.Gameplay.Demo1
{
    /// <summary>
    /// 卡牌品质阶级 (T1 - T4)
    /// </summary>
    public enum CardRank
    {
        Bronze = 0,
        Silver = 1,
        Gold = 2,
        Diamond = 3

    }

    /// <summary>
    /// 卡牌行为分类
    /// </summary>
    public enum CardType
    {
        Active,
        Passive
    }

    public enum GameState
    {
        None,
        Selection,
        Shop,
        Work,
        Treasure,
        BattleSelect,
        Battle,
        PlayerBattle,
        Reward,
        GameOver
    }

    public enum RoundPhase
    {
        Choose = 0,
        InEvent = 1,
        Complete = 2,
        End = 3
    }

    /// <summary>
    /// Demo1 业务常量定义
    /// </summary>
    public static class Demo1Const
    {
        /// <summary> 棋盘出战席最大极限格数 </summary>
        public const int MaxSlots = 10;
        public const int BenchMaxSlots = 10;
        public const int ShopRefreshCost = 2;
        public const float ShopEnterDelay = 0.35f;
        public const float ShopRefreshDelay = 0.25f;
        public const float ShopLeaveDelay = 0.2f;
        public const float BuyResolveDelay = 0.15f;
        public const float RoundAdvanceDelay = 0.4f;
        public const float PoisonTickInterval = 1f;
        public const float BattleEndDelay = 0.6f;
        /// <summary> 游戏开局拥有的初始有效格数 </summary>
        public const int InitialSlots = 4;
        /// <summary> 触发升级所需的经验值阈值 </summary>
        public const int ExpPerLevel = 8;
        /// <summary> 每回合结算时的基础低保金币收入 </summary>
        public const int DefaultGoldPerRound = 5;
        /// <summary> 玩家的初始全局声望上限 </summary>
        public const int MaxPrestige = 20;
        /// <summary> 胜利所需进度 </summary>
        public const int MaxProgress = 10;
    }
}
