using Framework;
using Game.Services;

namespace Game.Commands
{
    public class ChangeDiamondCommand : AbstractCommand
    {
        public int Amount;
        public string Reason;

        public override void Execute(object sender)
        {
            this.GetSystem<ResourceService>().RequestChangeDiamond(Amount, Reason);
        }
    }
}
