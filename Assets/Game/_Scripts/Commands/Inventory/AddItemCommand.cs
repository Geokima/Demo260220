using Framework;
using Game.Services;

namespace Game.Commands
{
    /// <summary>
    /// 添加物品命令
    /// </summary>
    public class AddItemCommand : AbstractCommand
    {
        /// <summary>物品配置ID</summary>
        public int ItemId;
        /// <summary>数量</summary>
        public int Amount;
        /// <summary>是否绑定</summary>
        public bool Bind;

        public override void Execute(object sender)
        {
            this.GetSystem<InventoryService>().RequestAddItem(ItemId, Amount, Bind);
        }
    }
}
