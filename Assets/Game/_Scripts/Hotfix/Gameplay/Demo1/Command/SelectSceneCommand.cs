using Framework;
using Game.Gameplay.Demo1.Event;

namespace Game.Gameplay.Demo1.Command
{
    public class SelectSceneCommand : AbstractCommand
    {
        public SceneMode Mode;
        public override void Execute(object sender)
        {
            this.SendEvent(new SelectSceneModeEvent(Mode));
        }
    }
}
