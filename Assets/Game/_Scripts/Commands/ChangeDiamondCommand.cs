using Framework;
using Game.Services;

namespace Game.Commands
{
    public class ChangeDiamondCommand : AbstractCommand
    {
        public long Amount;
        public string Reason;

        public override void Execute()
        {
            this.GetSystem<ResourceService>().RequestChangeDiamond(Amount, Reason);
        }
    }
}
