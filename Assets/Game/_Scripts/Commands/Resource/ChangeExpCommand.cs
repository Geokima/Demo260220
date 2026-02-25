using Framework;
using Game.Services;

namespace Game.Commands
{
    /// <summary>
    /// 变更经验命令
    /// </summary>
    public class ChangeExpCommand : AbstractCommand
    {
        /// <summary>变更数量（正数为增加，负数为减少）</summary>
        public int Amount;
        /// <summary>变更原因</summary>
        public string Reason;

        public override void Execute(object sender)
        {
            this.GetSystem<ResourceService>().RequestChangeExp(Amount, Reason);
        }
    }
}
