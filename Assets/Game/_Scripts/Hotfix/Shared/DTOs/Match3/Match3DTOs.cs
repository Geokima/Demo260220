using System;
using System.Collections.Generic;
using Game.Base;

namespace Game.DTOs
{
    // --- Start ---

    public class StartMatch3Request : BaseRequest
    {
        public int StageId { get; set; }
    }

    public class StartMatch3Response : BaseResponse<StartMatch3ResponseData> { }

    public class StartMatch3ResponseData
    {
        public int RandomSeed { get; set; } // 服务器下发的种子
    }

    // --- Finish ---

    public class FinishMatch3Request : BaseRequest
    {
        public int StageId { get; set; }
        public int FinalScore { get; set; }
        
        /// <summary>
        /// 操作序列，用于后端重放验证
        /// </summary>
        public List<Match3SwapAction> Actions { get; set; }
    }

    public class FinishMatch3Response : BaseResponse<FinishMatch3ResponseData> { }

    public class FinishMatch3ResponseData
    {
        public bool IsSuccess { get; set; }
        public int RewardGold { get; set; }
    }

    // --- Common ---

    public class Match3SwapAction
    {
        public int X1, Y1;
        public int X2, Y2;
        public long Timestamp; // 操作的时间戳
    }
}
