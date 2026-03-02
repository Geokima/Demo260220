using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Config;
using Framework.Modules.Scene;
using Framework.Modules.UI;
using Game.Configs;

namespace Game.Commands
{
    public class ChangeSceneCommand : AbstractCommand
    {
        public string SceneGroup { get; set; }

        public override void Execute(object sender)
        {
            this.GetSystem<IUISystem>().Open<UI_BlackScreen>();
            DelayLoadSceneAsync();
        }

        private async void DelayLoadSceneAsync()
        {
            await UniTask.Delay(1000);
            var sceneConfig = this.GetSystem<IConfigSystem>()
                .GetSheet<SceneConfig>()
                .FindBy(x => x.SceneGroup, SceneGroup);

            if (sceneConfig == null)
            {
                UnityEngine.Debug.LogError($"[ChangeSceneCommand] Scene config not found: {SceneGroup}");
                return;
            }

            this.GetSystem<ISceneSystem>().LoadScene(sceneConfig.AssetFullPath);
        }
    }
}
