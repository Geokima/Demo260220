using Framework;

namespace Game.Gameplay.Demo1.System
{
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
                    new RoundOption { Mode = SceneMode.Battle, Name = "战斗" }
                };
            }

            return new[]
            {
                new RoundOption { Mode = SceneMode.Shop, Name = "商店" },
                new RoundOption { Mode = SceneMode.Work, Name = "打工" },
                new RoundOption { Mode = SceneMode.Treasure, Name = "宝箱" }
            };
        }
    }
}
