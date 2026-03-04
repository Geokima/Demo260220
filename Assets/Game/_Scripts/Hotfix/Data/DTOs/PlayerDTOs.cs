using System;
using Newtonsoft.Json;

namespace Game.DTOs
{
    /// <summary>
    /// 玩家数据（用于资源同步）
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        [JsonProperty("diamond")]
        public int Diamond { get; set; }

        [JsonProperty("gold")]
        public int Gold { get; set; }

        [JsonProperty("exp")]
        public int Exp { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("energy")]
        public int Energy { get; set; }

        [JsonProperty("lastEnergyTime")]
        public long LastEnergyTime { get; set; }

        [JsonProperty("currentAmount")]
        public int CurrentAmount { get; set; }

        [JsonProperty("currentExp")]
        public int CurrentExp { get; set; }

        [JsonProperty("currentLevel")]
        public int CurrentLevel { get; set; }

        [JsonProperty("currentEnergy")]
        public int CurrentEnergy { get; set; }

        [JsonProperty("currentDiamond")]
        public int CurrentDiamond { get; set; }

        [JsonProperty("currentGold")]
        public int CurrentGold { get; set; }
    }

    /// <summary>
    /// 玩家数据响应
    /// </summary>
    public class PlayerResponse : BaseResponse<PlayerData> { }

    /// <summary>
    /// 背包数据响应
    /// </summary>
    public class InventoryResponse : BaseResponse<InventoryDTO> { }
}
