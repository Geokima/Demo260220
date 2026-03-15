using System.Collections.Generic;

namespace Game.Match3.Logic
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
        
        // 特殊方块
        LineHorizontal = 6, // 横向消除
        LineVertical = 7,   // 纵向消除
        Bomb = 8,           // 爆炸（九宫格）
        ColorBall = 9,       // 彩虹球（全消）

        Block = 10,         // 障碍物
    }

    /// <summary>
    /// 单元格数据
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
    /// 匹配结果详情
    /// </summary>
    public struct MatchResult
    {
        public List<Match3CellData> Cells;
        public Match3CellType CreateType; 
        public int CreatePointX;         
        public int CreatePointY;         
    }

    /// <summary>
    /// 掉落移动信息
    /// </summary>
    public struct FallInfo
    {
        public int FromX, FromY;
        public int ToX, ToY;
        public Match3CellType Type;
        public bool IsNew; 
    }

    public static class Match3Logic
    {
        public static List<MatchResult> FindMatches(Match3CellType[,] grid, int width, int height)
        {
            var results = new List<MatchResult>();
            var lines = new List<List<Match3CellData>>();

            // 1. 水平扫描
            for (int y = 0; y < height; y++)
            {
                int count = 1;
                for (int x = 1; x < width; x++)
                {
                    if (IsMatchable(grid[x, y]) && grid[x, y] == grid[x - 1, y])
                        count++;
                    else
                    {
                        if (count >= 3)
                        {
                            var list = new List<Match3CellData>();
                            for (int i = 1; i <= count; i++)
                                list.Add(new Match3CellData(x - i, y, grid[x - i, y]));
                            lines.Add(list);
                        }
                        count = 1;
                    }
                }
                if (count >= 3)
                {
                    var list = new List<Match3CellData>();
                    for (int i = 1; i <= count; i++)
                        list.Add(new Match3CellData(width - i, y, grid[width - i, y]));
                    lines.Add(list);
                }
            }

            // 2. 垂直扫描
            for (int x = 0; x < width; x++)
            {
                int count = 1;
                for (int y = 1; y < height; y++)
                {
                    if (IsMatchable(grid[x, y]) && grid[x, y] == grid[x, y - 1])
                        count++;
                    else
                    {
                        if (count >= 3)
                        {
                            var list = new List<Match3CellData>();
                            for (int i = 1; i <= count; i++)
                                list.Add(new Match3CellData(x, y - i, grid[x, y - i]));
                            lines.Add(list);
                        }
                        count = 1;
                    }
                }
                if (count >= 3)
                {
                    var list = new List<Match3CellData>();
                    for (int i = 1; i <= count; i++)
                        list.Add(new Match3CellData(x, height - i, grid[x, height - i]));
                    lines.Add(list);
                }
            }

            foreach (var line in lines)
            {
                var result = new MatchResult { Cells = line, CreateType = Match3CellType.None };
                if (line.Count == 4) result.CreateType = Match3CellType.LineHorizontal;
                else if (line.Count >= 5) result.CreateType = Match3CellType.ColorBall;
                
                if (result.CreateType != Match3CellType.None)
                {
                    result.CreatePointX = line[0].X;
                    result.CreatePointY = line[0].Y;
                }
                results.Add(result);
            }

            return results;
        }

        private static bool IsMatchable(Match3CellType type) => type != Match3CellType.None && type != Match3CellType.Block;

        public static List<FallInfo> CalculateFalls(Match3CellType[,] grid, int width, int height, System.Random random, List<Match3CellType> availableTypes)
        {
            var falls = new List<FallInfo>();
            bool changed = true;

            while (changed)
            {
                changed = false;
                for (int x = 0; x < width; x++)
                {
                    for (int y = 1; y < height; y++)
                    {
                        if (grid[x, y] != Match3CellType.None && grid[x, y] != Match3CellType.Block && grid[x, y - 1] == Match3CellType.None)
                        {
                            falls.Add(new FallInfo { FromX = x, FromY = y, ToX = x, ToY = y - 1, Type = grid[x, y], IsNew = false });
                            grid[x, y - 1] = grid[x, y];
                            grid[x, y] = Match3CellType.None;
                            changed = true;
                        }
                    }
                }

                if (!changed) 
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 1; y < height; y++)
                        {
                            if (grid[x, y] == Match3CellType.None)
                            {
                                foreach (int dx in new[] { -1, 1 })
                                {
                                    int nx = x + dx;
                                    if (nx >= 0 && nx < width && grid[nx, y] != Match3CellType.None && grid[nx, y] != Match3CellType.Block)
                                    {
                                        falls.Add(new FallInfo { FromX = nx, FromY = y, ToX = x, ToY = y, Type = grid[nx, y], IsNew = false });
                                        grid[x, y] = grid[nx, y];
                                        grid[nx, y] = Match3CellType.None;
                                        changed = true;
                                        break;
                                    }
                                }
                            }
                            if (changed) break;
                        }
                        if (changed) break;
                    }
                }
            }

            for (int x = 0; x < width; x++)
            {
                int emptyCount = 0;
                for (int y = height - 1; y >= 0; y--)
                {
                    if (grid[x, y] == Match3CellType.None) emptyCount++;
                    else break; 
                }

                for (int i = 0; i < emptyCount; i++)
                {
                    int targetY = height - emptyCount + i;
                    var newType = availableTypes[random.Next(availableTypes.Count)];
                    grid[x, targetY] = newType;
                    falls.Add(new FallInfo { FromX = x, FromY = height + i, ToX = x, ToY = targetY, Type = newType, IsNew = true });
                }
            }

            return falls;
        }

        public static void FillWithoutMatches(Match3CellType[,] grid, int width, int height, System.Random random, List<Match3CellType> availableTypes)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] == Match3CellType.Block) continue;
                    var validTypes = new List<Match3CellType>(availableTypes);
                    if (x >= 2 && grid[x - 1, y] == grid[x - 2, y]) validTypes.Remove(grid[x - 1, y]);
                    if (y >= 2 && grid[x, y - 1] == grid[x, y - 2]) validTypes.Remove(grid[x, y - 1]);
                    grid[x, y] = validTypes[random.Next(validTypes.Count)];
                }
            }
        }
    }
}
