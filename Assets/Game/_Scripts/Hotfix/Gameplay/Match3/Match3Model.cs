using Game.Shared.Logic.Match3;
using Framework;

namespace Game.Gameplay.Match3
{
    public class Match3Model : AbstractModel
    {
        public BindableProperty<int> Score = new BindableProperty<int>();
        public BindableProperty<int> RemainingTurns = new BindableProperty<int>();
        
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Match3CellType[,] Grid { get; private set; }

        public override void Init()
        {
            Score.Value = 0;
            RemainingTurns.Value = 0;
        }

        public void InitGrid(int width, int height)
        {
            Width = width;
            Height = height;
            Grid = new Match3CellType[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Grid[x, y] = Match3CellType.None;
                }
            }
        }

        public void Clear()
        {
            Score.Value = 0;
            RemainingTurns.Value = 0;
            Grid = null;
        }
    }
}
