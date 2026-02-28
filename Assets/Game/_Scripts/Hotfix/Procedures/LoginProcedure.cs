using Framework.Modules.Procedure;
using Framework.Modules.UI;
using UnityEngine;

namespace Game.Procedures
{
    /// <summary>
    /// 登录流程
    /// </summary>
    public class LoginProcedure : ProcedureBase
    {

        public override void OnEnter()
        {
            Debug.Log("[LoginProcedure] OnEnter - 显示登录UI");
            Architecture.GetSystem<UISystem>().Open<UI_LoginPanel>();
        }

        public override void OnExit()
        {
        }
    }
}
