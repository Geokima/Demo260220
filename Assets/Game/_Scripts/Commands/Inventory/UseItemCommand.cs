using Framework;
using Game.Services;

namespace Game.Commands
{
    /// <summary>
    /// 使用物品命令
    /// </summary>
    public class UseItemCommand : AbstractCommand
    {
        /// <summary>物品唯一ID</summary>
        public string Uid;
        /// <summary>数量</summary>
        public int Amount;

        public override void Execute(object sender)
        {
            this.GetSystem<InventoryService>().RequestUseItem(Uid, Amount);
        }
    }
}
