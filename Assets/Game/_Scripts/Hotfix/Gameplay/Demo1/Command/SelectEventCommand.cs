using Framework;

namespace Game.Gameplay.Demo1.Command
{
    public class SelectEventCommand : AbstractCommand
    {
        public RoundEventType EventType { get; set; }

        public override void Execute(object sender)
        {
            var model = Architecture.GetModel<Demo1Model>();
            model.CurrentRoundEvent.Value = EventType;
        }
    }
}
