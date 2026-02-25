using Framework;
using Game.Services;

namespace Game.Commands
{
    public class ExchangeDiamondForGoldCommand : AbstractCommand
    {
        public int DiamondAmount;
        public int GoldAmount;

        public override void Execute(object sender)
        {
            this.GetSystem<ResourceService>().RequestExchangeDiamondForGold(DiamondAmount, GoldAmount);
        }
    }
}
