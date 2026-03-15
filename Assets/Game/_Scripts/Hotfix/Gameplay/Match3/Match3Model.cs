using Framework;
using Game.Match3.Logic;
using System.Collections.Generic;
using Game.DTOs;

namespace Game.Match3
{
    public class Match3Model : AbstractModel
    {
        public BindableProperty<int> Score = new BindableProperty<int>(0);
        public BindableProperty<int> RemainingTurns = new BindableProperty<int>(0);
        public BindableProperty<bool> IsBusy = new BindableProperty<bool>(false);

        public int Width { get; set; }
        public int Height { get; set; }
        public Match3CellType[,] Grid { get; set; }

        public Dictionary<Match3CellType, int> TargetCounts = new Dictionary<Match3CellType, int>();
        
        public int RandomSeed { get; set; }
        public List<Match3SwapAction> ActionHistory = new List<Match3SwapAction>();

        public override void Init() { }

        public void Reset(int width, int height, int turns, int seed)
        {
            Width = width;
            Height = height;
            RemainingTurns.Value = turns;
            Score.Value = 0;
            RandomSeed = seed;
            IsBusy.Value = false;
            Grid = new Match3CellType[width, height];
            ActionHistory.Clear();
            TargetCounts.Clear();
        }
    }
}
