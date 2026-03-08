using System;
using Game.Base;
using Newtonsoft.Json;

namespace Game.DTOs
{
    #region Player

    [Serializable]
    public class PlayerData
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("diamond")]
        public int Diamond { get; set; }

        [JsonProperty("gold")]
        public int Gold { get; set; }

        [JsonProperty("exp")]
        public int Exp { get; set; }

        [JsonProperty("energy")]
        public int Energy { get; set; }

        [JsonProperty("lastEnergyRecoverTime")]
        public long LastEnergyRecoverTime { get; set; }

        [JsonProperty("serverTime")]
        public long ServerTime { get; set; }
    }

    public class PlayerResponse : BaseResponse<PlayerData> { }

    #endregion
}
