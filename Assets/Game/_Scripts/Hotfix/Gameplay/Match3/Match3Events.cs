using System.Collections.Generic;
using Game.Match3.Logic;

namespace Game.Match3
{
    public struct Match3BoardInitializedEvent { }

    public struct Match3SwapSuccessEvent
    {
        public int X1, Y1, X2, Y2;
    }

    public struct Match3SwapFailEvent
    {
        public int X1, Y1, X2, Y2;
    }

    public struct Match3MatchEvent
    {
        public List<MatchResult> Matches;
    }

    public struct Match3RefillEvent
    {
        public List<FallInfo> Falls;
    }

    public struct Match3GameOverEvent
    {
        public bool IsWin;
        public int Score;
    }
}
