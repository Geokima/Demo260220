using Framework.Modules.Procedure;
using Framework.Modules.Scene;
using Game.Scene;

namespace Game.Procedures
{
    public class Demo1Procedure : ProcedureBase
    {
        public override void OnEnter()
        {
            Architecture.SendCommand(this, new ChangeSceneCommand() { SceneGroup = "Demo1" });
            Architecture.RegisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete);
        }

        private void OnSceneLoadComplete(SceneLoadCompleteEvent e)
        {
            // TODO ?
            Architecture.UnregisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete);
        }
        
        public override void OnExit()
        {
            Architecture.UnregisterEvent<SceneLoadCompleteEvent>(OnSceneLoadComplete);
        }
    }
}