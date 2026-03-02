using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Procedure;
using Game.Commands;
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

            // TODO: 如果需要等待加载完成，可以监听事件或改用异步 Command
            // 目前暂时直接进入登录流程（假设加载很快）
            ChangeProcedure<LoginProcedure>();
        }
    }
}
