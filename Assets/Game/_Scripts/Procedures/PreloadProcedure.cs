using Framework.Modules.Procedure;
using Framework.Modules.Config;
using Framework.Modules.Res;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Procedures
{
    /// <summary>
    /// 预加载流程 - 加载配置和资源
    /// </summary>
    public class PreloadProcedure : ProcedureBase
    {
        public override ProcedureType Type => ProcedureType.Preload;

        public override void OnEnter()
        {
            Debug.Log("[PreloadProcedure] OnEnter");
            LoadAll().Forget();
        }

        private async UniTaskVoid LoadAll()
        {
            var configSystem = Architecture.GetSystem<ConfigSystem>();
            var resSystem = Architecture.GetSystem<ResSystem>();
            await configSystem.LoadConfigsFrom(resSystem.AssetLoader, "Config");
            Debug.Log("[PreloadProcedure] Configs loaded");

            ChangeProcedure(ProcedureType.Login);
        }

        public override void OnExit()
        {
            Debug.Log("[PreloadProcedure] OnExit");
        }
    }
}
