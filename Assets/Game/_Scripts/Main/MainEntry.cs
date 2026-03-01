using UnityEngine;
using YooAsset;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using System.Text;

namespace Game.Main
{
    public class MainEntry : MonoBehaviour
    {
        public static MainEntry Instance { get; private set; }
        [SerializeField] private EPlayMode playMode = EPlayMode.EditorSimulateMode;
        public float Progress = 0f;
        public float ActualProgress = 0f;

        private void Awake()
        {
            Instance = this;
#if UNITY_EDITOR
            gameObject.AddComponent<ConsoleToScreen>();
#endif
        }

        private async void Start()
        {
            AssetUpdater.Instance.OnInitSuccess += async () =>
            {
                await HotfixUpdater.Instance.LoadHotfixAsync();
                ActualProgress = 1f;
            };
            await AssetUpdater.Instance.CheckVersionAndDownloadAsync(playMode, "DefaultPackage");
        }

        public async void LaunchGame()
        {
            if (GameObject.Find("#Game Manager") != null)
                return;

            Debug.Log("[MainEntry] 启动游戏");
            var handle = AssetUpdater.Instance.DefaultPackage.LoadAssetAsync<GameObject>("Game Manager");
            await handle;
            Instantiate(handle.AssetObject as GameObject).name = "#Game Manager";
        }

        private void Update()
        {
            if (Progress < ActualProgress)
            {
                Progress += Time.deltaTime * 2f;
                if (Progress > ActualProgress)
                    Progress = ActualProgress;

            }
        }
    }
}
