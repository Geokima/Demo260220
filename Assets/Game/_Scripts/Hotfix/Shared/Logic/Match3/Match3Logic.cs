using System.Collections.Generic;

namespace Game.Shared.Logic.Match3
{
    /// <summary>
    /// 单元格类型
    /// </summary>
    public enum Match3CellType
    {
        None = 0,
        Red = 1,
        Blue = 2,
        Green = 3,
        Yellow = 4,
        Purple = 5,
        Block = 10,
    }

    /// <summary>
    /// 单元格数据（纯数据结构，用于逻辑计算）
    /// </summary>
    public struct Match3CellData
    {
        public int X;
        public int Y;
        public Match3CellType Type;

        public Match3CellData(int x, int y, Match3CellType type)
        {
            X = x;
            Y = y;
            Type = type;
        }
    }

    /// <summary>
    /// 掉落移动信息
    /// </summary>
    public struct FallInfo
    {
        public int FromX, FromY;
        public int ToX, ToY;
        public Match3CellType Type;
        public bool IsNew; // 是否是新生成的
    }

    /// <summary>
    /// 三消核心逻辑引擎 - 纯数学推演，不存储任何状态
    /// </summary>
    public static class Match3Logic
    {
        /// <summary>
        /// 全盘搜索所有可消除的匹配（3个及以上）
        /// </summary>
        public static List<List<Match3CellData>> FindMatches(Match3CellType[,] grid, int width, int height)
        {
            var matches = new List<List<Match3CellData>>();
            var visited = new bool[width, height];

            // 1. 水平扫描
            for (int y = 0; y < height; y++)
            {
                int count = 1;
                for (int x = 1; x < width; x++)
                {
                    if (grid[x, y] != Match3CellType.None && grid[x, y] != Match3CellType.Block && grid[x, y] == grid[x - 1, y])
                    {
                        count++;
                    }
                    else
                    {
                        if (count >= 3)
                        {
                            var list = new List<Match3CellData>();
                            for (int i = 1; i <= count; i++)
                                list.Add(new Match3CellData(x - i, y, grid[x - i, y]));
                            matches.Add(list);
                        }
                        count = 1;
                    }
                }
                if (count >= 3)
                {
                    var list = new List<Match3CellData>();
                    for (int i = 1; i <= count; i++)
                        list.Add(new Match3CellData(width - i, y, grid[width - i, y]));
                    matches.Add(list);
                }
            }

            // 2. 垂直扫描
            for (int x = 0; x < width; x++)
            {
                int count = 1;
                for (int y = 1; y < height; y++)
                {
                    if (grid[x, y] != Match3CellType.None && grid[x, y] != Match3CellType.Block && grid[x, y] == grid[x, y - 1])
                    {
                        count++;
                    }
                    else
                    {
                        if (count >= 3)
                        {
                            var list = new List<Match3CellData>();
                            for (int i = 1; i <= count; i++)
                                list.Add(new Match3CellData(x, y - i, grid[x, y - i]));
                            matches.Add(list);
                        }
                        count = 1;
                    }
                }
                if (count >= 3)
                {
                    var list = new List<Match3CellData>();
                    for (int i = 1; i <= count; i++)
                        list.Add(new Match3CellData(x, height - i, grid[x, height - i]));
                    matches.Add(list);
                }
            }

            return matches;
        }

        /// <summary>
        /// 计算棋盘掉落和补位，返回所有移动信息
        /// </summary>
        public static List<FallInfo> CalculateFalls(Match3CellType[,] grid, int width, int height, System.Random random, List<Match3CellType> availableTypes)
        {
            var falls = new List<FallInfo>();

            for (int x = 0; x < width; x++)
            {
                int emptySlots = 0;
                // 从下往上扫
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] == Match3CellType.None)
                    {
                        emptySlots++;
                    }
                    else if (emptySlots > 0 && grid[x, y] != Match3CellType.Block)
                    {
                        // 现有的掉到下面的空位
                        int targetY = y - emptySlots;
                        falls.Add(new FallInfo
                        {
                            FromX = x, FromY = y,
                            ToX = x, ToY = targetY,
                            Type = grid[x, y],
                            IsNew = false
                        });
                        grid[x, targetY] = grid[x, y];
                        grid[x, y] = Match3CellType.None;
                    }
                }

                // 补位（从顶端落下新生成的）
                for (int i = 0; i < emptySlots; i++)
                {
                    int targetY = height - emptySlots + i;
                    var newType = availableTypes[random.Next(availableTypes.Count)];
                    grid[x, targetY] = newType;
                    falls.Add(new FallInfo
                    {
                        FromX = x, FromY = height + i, // 虚拟的上方起始位置
                        ToX = x, ToY = targetY,
                        Type = newType,
                        IsNew = true
                    });
                }
            }

            return falls;
        }

        /// <summary>
        /// 随机填充棋盘，且保证初始状态没有可消除的
        /// </summary>
        public static void FillWithoutMatches(Match3CellType[,] grid, int width, int height, System.Random random, List<Match3CellType> availableTypes)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var validTypes = new List<Match3CellType>(availableTypes);
                    
                    // 检查左边两个
                    if (x >= 2 && grid[x - 1, y] == grid[x - 2, y])
                        validTypes.Remove(grid[x - 1, y]);
                    
                    // 检查下面两个
                    if (y >= 2 && grid[x, y - 1] == grid[x, y - 2])
                        validTypes.Remove(grid[x, y - 1]);
                    
                    grid[x, y] = validTypes[random.Next(validTypes.Count)];
                }
            }
        }
    }
}
