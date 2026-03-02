using System;
using UnityEngine;
using YooAsset;
using Cysharp.Threading.Tasks;

namespace Game.Main.NewLauncher
{
    /// <summary>
    /// 整个项目的唯一 AOT 入口 - 负责驱动启动流程并提供按钮点击进入游戏的引导
    /// </summary>
    public class Launcher : MonoBehaviour
    {
        [SerializeField] private EPlayMode playMode = EPlayMode.EditorSimulateMode;
        [SerializeField] private string packageName = "DefaultPackage";

        // 静态事件，供 LauncherUI 订阅
        public static event Action<float, string> OnProgressUpdate;

        private async void Start()
        {
            try
            {
                // 1. 初始化资源系统 (0% - 10%)
                await LauncherAssets.InitAsync(playMode, packageName, OnProgressUpdate);

                // 2. 检查更新并下载 (10% - 90%)
                await LauncherAssets.UpdateAsync(playMode, OnProgressUpdate);

                // 3. 加载热更 DLL (90% - 95%)
                await LauncherHotfix.LoadAsync(OnProgressUpdate);

                // 4. 流程结束，通知 UI 激活按钮 (95% - 100%)
                OnProgressUpdate?.Invoke(1f, "点击任意位置进入游戏");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Launcher] 启动失败: {e.Message}");
                OnProgressUpdate?.Invoke(0f, $"<color=red>启动失败: {e.Message}</color>");
            }
        }

        /// <summary>
        /// 按钮点击事件 - 正式进入游戏业务逻辑
        /// </summary>
        public async void EnterGame()
        {
            // 对齐原有逻辑：加载并实例化 "Game Manager" 预制体
            Debug.Log("[Launcher] 正在从 YooAsset 加载 'Game Manager' 预制体...");
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
