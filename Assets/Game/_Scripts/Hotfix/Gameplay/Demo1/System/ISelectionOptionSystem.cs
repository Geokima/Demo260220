using Framework;

namespace Game.Gameplay.Demo1.System
{
    public interface ISelectionOptionSystem : ISystem
    {
        RoundOption[] GetOptions();
    }

    public struct RoundOption
    {
        public SceneMode Mode;
        public string Name;
        public string Description;
    }
}
