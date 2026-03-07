using System.Collections.Generic;
using Framework;

namespace Game.Inventory
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
        /// <summary>参数</summary>
        public Dictionary<string, string> Parameters;

        public override void Execute(object sender)
        {
            this.GetSystem<InventoryService>().RequestUseItem(Uid, Amount, Parameters);
        }
    }
}
