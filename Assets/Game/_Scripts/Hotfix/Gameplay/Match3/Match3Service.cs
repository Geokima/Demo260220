using Framework;
using Game.Match3.Logic;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Game.Match3
{
    public interface IMatch3Service : ISystem
    {
        void StartLevel(int width, int height, int turns, int seed);
        bool Swap(int x1, int y1, int x2, int y2);
    }

    public class Match3Service : AbstractSystem, IMatch3Service
    {
        private Match3Model mModel;
        private System.Random mRandom;

        public override void Init()
        {
            mModel = this.GetModel<Match3Model>();
        }

        public void StartLevel(int width, int height, int turns, int seed)
        {
            mModel.Reset(width, height, turns, seed);
            mRandom = new System.Random(seed);
            var availableTypes = new List<Match3CellType> { 
                Match3CellType.Red, Match3CellType.Blue, Match3CellType.Green, 
                Match3CellType.Yellow, Match3CellType.Purple 
            };
            Match3Logic.FillWithoutMatches(mModel.Grid, width, height, mRandom, availableTypes);
            this.SendEvent(new Match3BoardInitializedEvent());
        }

        public bool Swap(int x1, int y1, int x2, int y2)
        {
            if (mModel.IsBusy.Value || mModel.RemainingTurns.Value <= 0) return false;
            if (Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2) != 1) return false;
            mModel.ActionHistory.Add(new DTOs.Match3SwapAction { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Timestamp = System.DateTime.Now.Ticks });
            ProcessSwap(x1, y1, x2, y2);
            return true;
        }

        private async void ProcessSwap(int x1, int y1, int x2, int y2)
        {
            mModel.IsBusy.Value = true;
            SwapCells(x1, y1, x2, y2);
            mModel.RemainingTurns.Value--;
            var matches = Match3Logic.FindMatches(mModel.Grid, mModel.Width, mModel.Height);
            if (matches.Count > 0)
            {
                this.SendEvent(new Match3SwapSuccessEvent { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 });
                await ProcessMatchingCycle();
            }
            else
            {
                SwapCells(x1, y1, x2, y2);
                this.SendEvent(new Match3SwapFailEvent { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2 });
            }
            mModel.IsBusy.Value = false;
            CheckGameOver();
        }

        private async System.Threading.Tasks.Task ProcessMatchingCycle()
        {
            while (true)
            {
                var matches = Match3Logic.FindMatches(mModel.Grid, mModel.Width, mModel.Height);
                if (matches.Count == 0) break;
                foreach (var match in matches)
                {
                    foreach (var cell in match.Cells)
                    {
                        mModel.Grid[cell.X, cell.Y] = Match3CellType.None;
                        UpdateGoal(cell.Type);
                    }
                    mModel.Score.Value += match.Cells.Count * 10;
                }
                this.SendEvent(new Match3MatchEvent { Matches = matches });
                var availableTypes = new List<Match3CellType> { 
                    Match3CellType.Red, Match3CellType.Blue, Match3CellType.Green, 
                    Match3CellType.Yellow, Match3CellType.Purple 
                };
                var falls = Match3Logic.CalculateFalls(mModel.Grid, mModel.Width, mModel.Height, mRandom, availableTypes);
                this.SendEvent(new Match3RefillEvent { Falls = falls });
                await System.Threading.Tasks.Task.Delay(500); 
            }
        }

        private void UpdateGoal(Match3CellType type)
        {
            if (mModel.TargetCounts.ContainsKey(type) && mModel.TargetCounts[type] > 0)
                mModel.TargetCounts[type]--;
        }

        private void CheckGameOver()
        {
            bool isSuccess = mModel.TargetCounts.Values.All(v => v <= 0) || mModel.Score.Value >= 1000;
            if (isSuccess || mModel.RemainingTurns.Value <= 0)
                this.SendEvent(new Match3GameOverEvent { IsWin = isSuccess, Score = mModel.Score.Value });
        }

        private void SwapCells(int x1, int y1, int x2, int y2)
        {
            var temp = mModel.Grid[x1, y1];
            mModel.Grid[x1, y1] = mModel.Grid[x2, y2];
            mModel.Grid[x2, y2] = temp;
        }
    }
}
