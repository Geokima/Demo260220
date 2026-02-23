using Framework;
using Game.Services;

namespace Game.Commands
{
    /// <summary>
    /// 获取背包命令
    /// </summary>
    public class GetInventoryCommand : AbstractCommand
    {
        public override void Execute()
        {
            this.GetSystem<InventoryService>().RequestGetInventory();
        }
    }

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

        public override void Execute()
        {
            this.GetSystem<InventoryService>().RequestAddItem(ItemId, Amount, Bind);
        }
    }

    /// <summary>
    /// 移除物品命令
    /// </summary>
    public class RemoveItemCommand : AbstractCommand
    {
        /// <summary>物品唯一ID</summary>
        public string Uid;
        /// <summary>数量</summary>
        public int Amount;

        public override void Execute()
        {
            this.GetSystem<InventoryService>().RequestRemoveItem(Uid, Amount);
        }
    }

    /// <summary>
    /// 使用物品命令
    /// </summary>
    public class UseItemCommand : AbstractCommand
    {
        /// <summary>物品唯一ID</summary>
        public string Uid;
        /// <summary>数量</summary>
        public int Amount;

        public override void Execute()
        {
            this.GetSystem<InventoryService>().RequestUseItem(Uid, Amount);
        }
    }
}
