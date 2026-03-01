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

        public override void OnEnter()
        {
            LoadAll().Forget();
        }

        private async UniTaskVoid LoadAll()
        {
            var configSystem = Architecture.GetSystem<ConfigSystem>();
            var resSystem = Architecture.GetSystem<ResSystem>();
            await configSystem.LoadConfigsFrom(resSystem.AssetLoader);
            Debug.Log("[PreloadProcedure] Configs loaded");

            // 预加载完成后进入登录流程
            ChangeProcedure<LoginProcedure>();
        }

        public override void OnExit()
        {
        }
    }
}
