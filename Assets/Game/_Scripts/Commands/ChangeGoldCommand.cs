using Framework;
using Game.Services;

namespace Game.Commands
{
    public class ChangeGoldCommand : AbstractCommand
    {
        public long Amount;
        public string Reason;

        public override void Execute()
        {
            this.GetSystem<ResourceService>().RequestChangeGold(Amount, Reason);
        }
    }
}
