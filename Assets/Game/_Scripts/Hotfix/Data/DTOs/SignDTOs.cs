using System;
using System.Collections.Generic;

namespace Hotfix.Data.DTOs
{
    [Serializable]
    public class SignData
    {
        public DateTime SignInDate { get; set; }
        public int RewardId { get; set; }
    }

    [Serializable]
    public class SignRewardData
    {
        public int RewardId { get; set; }
        public string Description { get; set; }
        public int Amount { get; set; }
    }

    [Serializable]
    public class SignResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<SignData> SignDataList { get; set; }
    }
}