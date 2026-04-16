using Framework.Modules.Procedure;
using Framework.Modules.Scene;
using Framework.Modules.UI;
using Game.Scene;
using Cysharp.Threading.Tasks;
using Game.Auth;

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
            Architecture.RegisterEvent<LoginSuccessEvent>(OnLoginSuccess);
        }

        private async void OnSceneLoadComplete(SceneLoadCompleteEvent e)
        {
            Architecture.UnregisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete);
            await UniTask.Delay(1000);
            Architecture.GetSystem<IUISystem>().Open<UI_LoginPanel>();
        }

        private void OnLoginSuccess(LoginSuccessEvent e)
        {
            Architecture.UnregisterEvent<LoginSuccessEvent>(OnLoginSuccess);
            Architecture.GetSystem<IProcedureSystem>().ChangeProcedure<MainProcedure>();
        }
    }
}
