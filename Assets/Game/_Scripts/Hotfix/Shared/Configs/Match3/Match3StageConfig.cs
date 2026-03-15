using System.Collections.Generic;

namespace Game.Shared.Configs
{
    /// <summary>
    /// 共用数据结构：三消关卡配置表
    /// </summary>
    public class Match3StageConfig
    {
        public int StageId { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Turns { get; set; }
        
        // 可用方块种类
        public List<int> AvailableTypes { get; set; }
        
        // 初始格子预设（如果是阻挡物或预设方块可以用二维数组存储，这里暂简）
        public int[,] InitialGrid { get; set; }

        // 过关目标（例如要消除多少个红色的）
        public Dictionary<int, int> Targets { get; set; } 
    }
}
