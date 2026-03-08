using System.Collections.Generic;
using Framework;

namespace Game.Inventory
{
    /// <summary> 添加物品命令 </summary>
    public class AddItemCommand : AbstractCommand
    {
        public int ItemId;
        public int Amount;
        public bool Bind;

        public override void Execute(object sender)
        {
            this.GetSystem<InventoryService>().RequestAddItem(ItemId, Amount, Bind);
        }
    }

    /// <summary> 获取背包命令 </summary>
    public class GetInventoryCommand : AbstractCommand
    {
        public override void Execute(object sender)
        {
            this.GetSystem<InventoryService>().RequestGetInventory();
        }
    }

    /// <summary> 移除物品命令 </summary>
    public class RemoveItemCommand : AbstractCommand
    {
        public string Uid;
        public int Amount;

        public override void Execute(object sender)
        {
            this.GetSystem<InventoryService>().RequestRemoveItem(Uid, Amount);
        }
    }

    /// <summary> 使用物品命令 </summary>
    public class UseItemCommand : AbstractCommand
    {
        public string Uid;
        public int Amount;
        public Dictionary<string, string> Parameters;

        public override void Execute(object sender)
        {
            this.GetSystem<InventoryService>().RequestUseItem(Uid, Amount, Parameters);
        }
    }
}
