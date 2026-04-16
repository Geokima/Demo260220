using Framework.Modules.Procedure;
using Framework.Modules.UI;
using UnityEngine;
using Game.Scene;
using Framework.Modules.Scene;
using Cysharp.Threading.Tasks;

namespace Game.Procedures
{
    /// <summary>
    /// 主游戏流程
    /// </summary>
    public class MainProcedure : ProcedureBase
    {

        public override void OnEnter()
        {
            Architecture.SendCommand(this, new ChangeSceneCommand() { SceneGroup = "Main" });
            Architecture.RegisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete);
        }
        private async void OnSceneLoadComplete(SceneLoadCompleteEvent e)
        {
            Architecture.UnregisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete);
            await UniTask.Delay(1000);
            Architecture.GetSystem<IUISystem>().Open<UI_MainPanel>();
        }
    }
}
