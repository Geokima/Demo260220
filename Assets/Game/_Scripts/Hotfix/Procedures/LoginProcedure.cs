using Framework.Modules.Procedure;
using Framework.Modules.Scene;
using Framework.Modules.UI;
using Game.Commands;
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
            Architecture.SendCommand(this, new ChangeSceneCommand() { SceneGroup = "Login" });
            Architecture.RegisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete);
        }

        private void OnSceneLoadComplete(SceneLoadCompleteEvent e)
        {
            Architecture.GetSystem<IUISystem>().Open<UI_LoginPanel>();
            Architecture.UnRegisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete);
        }
    }
}
