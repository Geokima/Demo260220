using UnityEngine;
using YooAsset;
using Cysharp.Threading.Tasks;

namespace Game.Main
{
    public class MainEntry : MonoBehaviour
    {
        [SerializeField] private EPlayMode playMode = EPlayMode.EditorSimulateMode;

        private void Awake()
        {
#if UNITY_EDITOR
            gameObject.AddComponent<ConsoleToScreen>();
#endif
        }

        private void Start()
        {
            AssetUpdater.Instance.OnInitSuccess += LaunchGame;
            AssetUpdater.Instance.CheckVersionAndDownloadAsync(playMode, "DefaultPackage").Forget();
        }

        private async void LaunchGame()
        {
            await HotfixUpdater.Instance.LoadHotfixAsync();
            Debug.Log("[MainEntry] 启动游戏");
            var handle = AssetUpdater.Instance.DefaultPackage.LoadAssetAsync<GameObject>("Game Manager");
            await handle;
            Instantiate(handle.AssetObject as GameObject).name = "#Game Manager";
            //Destroy(gameObject);
        }
    }
}
