using Framework;
using Game.Services;

namespace Game.Commands
{
    /// <summary>
    /// 获取背包命令
    /// </summary>
    public class GetInventoryCommand : AbstractCommand
    {
        public override void Execute(object sender)
        {
            this.GetSystem<InventoryService>().RequestGetInventory();
        }
    }
}
