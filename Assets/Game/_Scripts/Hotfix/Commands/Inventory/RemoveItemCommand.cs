using Framework;
using Game.Services;

namespace Game.Commands
{
    /// <summary>
    /// 移除物品命令
    /// </summary>
    public class RemoveItemCommand : AbstractCommand
    {
        /// <summary>物品唯一ID</summary>
        public string Uid;
        /// <summary>数量</summary>
        public int Amount;

        public override void Execute(object sender)
        {
            this.GetSystem<InventoryService>().RequestRemoveItem(Uid, Amount);
        }
    }
}
