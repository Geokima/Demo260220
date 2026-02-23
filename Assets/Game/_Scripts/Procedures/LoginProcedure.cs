using Framework.Modules.Procedure;
using UnityEngine;

namespace Game.Procedures
{
    /// <summary>
    /// 登录流程
    /// </summary>
    public class LoginProcedure : ProcedureBase
    {
        public override ProcedureType Type => ProcedureType.Login;

        public override void OnEnter()
        {
            Debug.Log("[LoginProcedure] OnEnter - 显示登录UI");
        }

        public override void OnExit()
        {
            Debug.Log("[LoginProcedure] OnExit");
        }
    }
}
