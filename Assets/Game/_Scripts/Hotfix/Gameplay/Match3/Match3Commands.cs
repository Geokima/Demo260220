using Framework;

namespace Game.Gameplay.Match3
{
    /// <summary> 开始三消关卡命令 </summary>
    public class StartMatch3LevelCommand : AbstractCommand
    {
        public int Width = 8;
        public int Height = 8;
        public int Turns = 20;

        public override void Execute(object sender)
        {
            this.GetSystem<Match3Service>().StartStage(Width, Height, Turns);
        }
    }

    /// <summary> 交换单元格命令 </summary>
    public class Match3SwapCommand : AbstractCommand
    {
        public int X1, Y1, X2, Y2;

        public override void Execute(object sender)
        {
            this.GetSystem<Match3Service>().Swap(X1, Y1, X2, Y2);
        }
    }
}
