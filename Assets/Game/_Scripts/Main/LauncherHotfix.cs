using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Game.Main
{
    public static class LauncherHotfix
    {
        private static readonly List<string> AotMetaAssemblyFiles = new()
        {
            "mscorlib",
            "System",
            "System.Core",
            "UniTask",
            "UnityEngine.CoreModule",
        };

        public static async UniTask LoadAsync(Action<float, string> onProgress)
        {
#if UNITY_EDITOR
            Debug.Log("[LauncherHotfix] 编辑器模式，跳过 DLL 热更新");
            onProgress?.Invoke(0.95f, "编辑器模式：跳过 DLL 热更新...");
            await UniTask.Delay(100);
#else
            Debug.Log("[LauncherHotfix] 开始加载热更新环境...");
            onProgress?.Invoke(0.91f, "正在加载 AOT 元数据...");
            await LoadMetadataForAOTAssemblies();

            onProgress?.Invoke(0.93f, "正在加载热更新 DLL...");
            await LoadHotfixAssembly();
            Debug.Log("[LauncherHotfix] 热更新环境加载完成");
#endif
            onProgress?.Invoke(0.95f, "热更新加载完成");
        }

        private static async UniTask LoadMetadataForAOTAssemblies()
        {
            Debug.Log("[LauncherHotfix] 开始加载 AOT 元数据...");
            foreach (var aotDllName in AotMetaAssemblyFiles)
            {
                var op = LauncherAssets.DefaultPackage.LoadAssetAsync<TextAsset>($"Assets/Game/Download/Hotfix/{aotDllName}.dll.bytes");
                await op;
                Debug.Log($"[LauncherHotfix] 已加载 AOT 元数据: {aotDllName}");
            }
            Debug.Log("[LauncherHotfix] AOT 元数据加载完成");
        }

        private static async UniTask LoadHotfixAssembly()
        {
            Debug.Log("[LauncherHotfix] 开始加载热更新 DLL...");
            var op = LauncherAssets.DefaultPackage.LoadAssetAsync<TextAsset>("Assets/Game/Download/Hotfix/Hotfix.dll.bytes");
            await op;
            System.Reflection.Assembly.Load((op.AssetObject as TextAsset).bytes);
            Debug.Log("[LauncherHotfix] 热更新 DLL 加载完成并注入内存");
        }
    }
}
