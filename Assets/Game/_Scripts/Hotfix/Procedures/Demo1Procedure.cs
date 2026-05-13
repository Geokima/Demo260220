using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Framework.Modules.Procedure;
using Framework.Modules.Res;
using Framework.Modules.Scene;
using Game.Scene;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Procedures
{
    public class Demo1Procedure : ProcedureBase
    {
        public override void OnEnter()
        {
            Architecture.SendCommand(this, new ChangeSceneCommand() { SceneGroup = "Demo1" });
            Architecture.RegisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete);
        }
        
        private async void OnSceneLoadComplete(SceneLoadCompleteEvent e)
        {
            Architecture.UnregisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete);
            await UniTask.DelayFrame(1);
            var entry = Owner.Architecture.GetSystem<IResSystem>().Load<GameObject>("Demo1Entry");
            if (entry)
                Object.Instantiate(entry);
        }
        
        public override void OnExit()
        {
            Architecture.UnregisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete);
        }
    }
}