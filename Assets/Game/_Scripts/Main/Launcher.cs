using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Game.Main
{
    public class Launcher : MonoBehaviour
    {
        [SerializeField] private EPlayMode playMode = EPlayMode.EditorSimulateMode;
        [SerializeField] private string packageName = "DefaultPackage";

        public static event Action<float, string> OnProgressUpdate;

        private async void Start()
        {
            try
            {
                await LauncherAssets.InitAsync(playMode, packageName, OnProgressUpdate);
                await LauncherAssets.UpdateAsync(playMode, OnProgressUpdate);
                await LauncherHotfix.LoadAsync(OnProgressUpdate);
                OnProgressUpdate?.Invoke(1f, "点击任意位置进入游戏");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Launcher] 启动失败: {e.Message}");
                OnProgressUpdate?.Invoke(0f, $"<color=red>启动失败: {e.Message}</color>");
            }
        }

        public async void EnterGame()
        {
            Debug.Log("[Launcher] 正在加载 'Game Manager' 预制体...");
            var handle = LauncherAssets.DefaultPackage.LoadAssetAsync<GameObject>("Game Manager");
            await handle;

            if (handle.AssetObject != null)
            {
                Debug.Log("[Launcher] 'Game Manager' 加载成功，正在实例化...");
                var go = Instantiate(handle.AssetObject as GameObject);
                go.name = "#Game Manager";
                Debug.Log("[Launcher] 游戏环境交接完成，销毁启动器...");
                Destroy(gameObject);
            }
            else
            {
                Debug.LogError("[Launcher] 无法加载 'Game Manager' 预制体！请检查资源配置。");
            }
        }
    }
}
