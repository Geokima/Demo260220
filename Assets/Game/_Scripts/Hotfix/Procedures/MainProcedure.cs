using Framework.Modules.Procedure;
using UnityEngine;

namespace Game.Procedures
{
    /// <summary>
    /// 主游戏流程
    /// </summary>
    public class MainProcedure : ProcedureBase
    {

        public override void OnEnter()
        {
            Debug.Log("[MainProcedure] OnEnter - 进入主游戏");
        }

        public override void OnExit()
        {
            Debug.Log("[MainProcedure] OnExit");
        }
    }
}
