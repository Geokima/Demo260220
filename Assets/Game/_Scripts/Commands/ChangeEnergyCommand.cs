using Framework;
using Game.Services;

namespace Game.Commands
{
    /// <summary>
    /// 变更体力命令
    /// </summary>
    public class ChangeEnergyCommand : AbstractCommand
    {
        /// <summary>变更数量（正数为增加，负数为减少）</summary>
        public long Amount;
        /// <summary>变更原因</summary>
        public string Reason;

        public override void Execute()
        {
            this.GetSystem<ResourceService>().RequestChangeEnergy(Amount, Reason);
        }
    }
}
