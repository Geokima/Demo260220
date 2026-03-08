using System.Collections.Generic;
using Framework;
using Game.Shared.Logic.Match3;
using UnityEngine;

namespace Game.Gameplay.Match3
{
    public class Match3Service : AbstractSystem
    {
        private Match3Model _model;
        private System.Random _random = new System.Random();
        private List<Match3CellType> _availablePool = new List<Match3CellType> 
        { 
            Match3CellType.Red, Match3CellType.Blue, Match3CellType.Green, 
            Match3CellType.Yellow, Match3CellType.Purple 
        };

        public override void Init()
        {
            _model = this.GetModel<Match3Model>();
        }

        /// <summary>
        /// 开始关卡
        /// </summary>
        public void StartStage(int width, int height, int turns)
        {
            _model.InitGrid(width, height);
            _model.RemainingTurns.Value = turns;
            _model.Score.Value = 0;

            // 初始填充（且保证无初始匹配）
            Match3Logic.FillWithoutMatches(_model.Grid, width, height, _random, _availablePool);

            // 发送初始化完成事件
            this.SendEvent(new Match3BoardInitializedEvent());
        }

        /// <summary>
        /// 玩家执行交换
        /// </summary>
        public void Swap(int x1, int y1, int x2, int y2)
        {
            if (_model.RemainingTurns.Value <= 0) return;

            // 0. 坐标合法性与邻居检测
            if (!IsAdjacent(x1, y1, x2, y2)) return;

            // 1. 尝试交换并查找匹配
            var grid = _model.Grid;
            
            // 备份（简易实现，实际可优化）
            var originalT1 = grid[x1, y1];
            var originalT2 = grid[x2, y2];

            if (originalT1 == Match3CellType.Block || originalT2 == Match3CellType.Block) return;

            // 执行逻辑交换
            grid[x1, y1] = originalT2;
            grid[x2, y2] = originalT1;

            var matches = Match3Logic.FindMatches(grid, _model.Width, _model.Height);

            if (matches.Count > 0)
            {
                // 成功匹配
                _model.RemainingTurns.Value--;
                
                // 发送交换成功事件（表现层会播动画）
                this.SendEvent(new Match3SwapSuccessEvent { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 });

                // 开始递归处理消除
                ProcessCycle();
            }
            else
            {
                // 无效交换，换回来
                grid[x1, y1] = originalT1;
                grid[x2, y2] = originalT2;
                
                // 发送交换失败/撤回事件
                this.SendEvent(new Match3SwapFailEvent { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 });
            }
        }

        private bool IsAdjacent(int x1, int y1, int x2, int y2)
        {
            return (Mathf.Abs(x1 - x2) == 1 && y1 == y2) || (Mathf.Abs(y1 - y2) == 1 && x1 == x2);
        }

        /// <summary>
        /// 递归处理消除和下落逻辑
        /// </summary>
        private void ProcessCycle()
        {
            var matches = Match3Logic.FindMatches(_model.Grid, _model.Width, _model.Height);
            if (matches.Count == 0)
            {
                // 检查是否输了或者没步数了
                CheckGameOver();
                return;
            }

            // 1. 计算分数并标记消除
            int totalCleared = 0;
            HashSet<(int, int)> clearedPositions = new HashSet<(int, int)>();
            foreach (var match in matches)
            {
                foreach (var cell in match)
                {
                    if (clearedPositions.Add((cell.X, cell.Y)))
                    {
                        totalCleared++;
                        _model.Grid[cell.X, cell.Y] = Match3CellType.None;
                    }
                }
            }
            
            _model.Score.Value += totalCleared * 10; // 简易计分

            // 发送消除事件
            this.SendEvent(new Match3MatchEvent { MatchedCells = matches });

            // 2. 计算下落
            var fallInfos = Match3Logic.CalculateFalls(_model.Grid, _model.Width, _model.Height, _random, _availablePool);
            
            // 发送下落填充事件
            this.SendEvent(new Match3RefillEvent { Falls = fallInfos });

            // 3. 继续下一轮检测（会有表现层的时间差，但在逻辑层可瞬间计算或延迟）
            // 这里我们发一个延迟继续的请求，或者直接递归
            // 实际上表现层应该通过 Callback 通知逻辑层继续，或者逻辑层等待表现完成
            // 为了架构纯粹，我们这里通过事件让 View 播放完再回调 Command 继续 ProcessCycle
        }

        private void CheckGameOver()
        {
            if (_model.RemainingTurns.Value <= 0)
            {
                // 游戏结束
                this.SendEvent(new Match3GameOverEvent { IsWin = _model.Score.Value >= 1000 }); // 简易胜利阈值
            }
        }
    }
}
