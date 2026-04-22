using Framework;

namespace Game.Gameplay.Demo1.System
{
    public interface ISelectionOptionSystem : ISystem
    {
        RoundOption[] GetOptions();
    }

    public struct RoundOption
    {
        public GameState State;
        public string Name;
        public int Data;
    }

    public class SelectionOptionSystem : AbstractSystem, ISelectionOptionSystem
    {
        public RoundOption[] GetOptions()
        {
            var model = this.GetModel<Demo1Model>();
            int round = model.Round.Value;

            if (round == 3 || round == 6)
            {
                return new[]
                {
                    new RoundOption { State = GameState.Battle, Name = "战斗", Data = 1 }
                };
            }

            return new[]
            {
                new RoundOption { State = GameState.Shop, Name = "商店", Data = 0 },
                new RoundOption { State = GameState.Work, Name = "打工", Data = 0 },
                new RoundOption { State = GameState.Treasure, Name = "宝箱", Data = 0 }
            };
        }
    }
}
