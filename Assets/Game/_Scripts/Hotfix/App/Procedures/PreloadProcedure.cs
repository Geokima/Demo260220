using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Procedure;
using Game.Config;
using UnityEngine;

namespace Game.Procedures
{
    /// <summary>
    /// 预加载流程 - 加载配置和资源
    /// </summary>
    public class PreloadProcedure : ProcedureBase
    {
        public override void OnEnter()
        {
            this.SendCommand(new LoadAllConfigsCommand());
            Debug.Log("[PreloadProcedure] Started loading configs via Command");
            this.RegisterEvent<ConfigLoadedEvent>(OnConfigLoaded);
        }

        private void OnConfigLoaded(ConfigLoadedEvent @event)
        {
            this.UnregisterEvent<ConfigLoadedEvent>(OnConfigLoaded);
            Debug.Log("[PreloadProcedure] Configs loaded, switching to LoginProcedure");
            ChangeProcedure<LoginProcedure>();
        }
    }
}
