namespace Game.Gameplay.Demo1.Event
{
    public struct CollectRewardEvent { }

    public struct BattleEndedEvent
    {
        public bool PlayerWon;
    }

    public struct GameStateChangedEvent
    {
        public GameState State;
    }
}
