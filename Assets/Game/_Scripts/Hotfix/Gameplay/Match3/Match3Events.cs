using Framework;
using System.Collections.Generic;
using Game.Shared.Logic.Match3;

namespace Game.Gameplay.Match3
{
    /// <summary> 棋盘初始化完成事件 </summary>
    public struct Match3BoardInitializedEvent : IEvent { }

    /// <summary> 交换成功事件 </summary>
    public struct Match3SwapSuccessEvent : IEvent
    {
        public int X1, Y1, X2, Y2;
    }

    /// <summary> 交换失败/回退事件 </summary>
    public struct Match3SwapFailEvent : IEvent
    {
        public int X1, Y1, X2, Y2;
    }

    /// <summary> 匹配消除事件（含消除列表） </summary>
    public struct Match3MatchEvent : IEvent
    {
        public List<List<Match3CellData>> MatchedCells;
    }

    /// <summary> 下落与补位事件 </summary>
    public struct Match3RefillEvent : IEvent
    {
        public List<FallInfo> Falls;
    }

    /// <summary> 玩法关卡结束事件 </summary>
    public struct Match3GameOverEvent : IEvent
    {
        public bool IsWin;
        public int FinalScore;
    }
}
