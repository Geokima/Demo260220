using UnityEngine;
using YooAsset;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;

namespace Game.Main
{
    public class HotfixUpdater
    {
        private static HotfixUpdater _instance;
        public static HotfixUpdater Instance => _instance ??= new HotfixUpdater();

        public static List<string> AotMetaAssemblyFiles = new()
        {
            "mscorlib",
            "System",
            "System.Core",
            "UniTask",
            "UnityEngine.CoreModule",
        };


        private HotfixUpdater() { }

        public async UniTask LoadHotfixAsync()
        {
#if UNITY_EDITOR
            Debug.Log("[HotfixUpdater] 编辑器模式，跳过DLL热更新");
            await UniTask.CompletedTask;
#else
            Debug.Log("[HotfixUpdater] 开始加载DLL热更新");

            await LoadMetadataForAOTAssemblies();
            await LoadHotfixAssembly();

            Debug.Log("[HotfixUpdater] DLL热更新完成");
#endif
        }

        private async UniTask LoadMetadataForAOTAssemblies()
        {
            // HomologousImageMode mode = HomologousImageMode.SuperSet;
            foreach (var aotDllName in AotMetaAssemblyFiles)
            {
                var operation = AssetUpdater.Instance.DefaultPackage.LoadAssetAsync($"Assets/Game/Download/Hotfix/{aotDllName}.dll.bytes");
                await operation.Task;
                var asset = operation.AssetObject as TextAsset;
                // LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(asset.bytes, mode);
            }
            Debug.Log("[HotfixUpdater] AOT元数据加载完成");
        }

        private async UniTask LoadHotfixAssembly()
        {
            var operation = AssetUpdater.Instance.DefaultPackage.LoadAssetAsync("Assets/Game/Download/Hotfix/Hotfix.dll.bytes");
            await operation.Task;
            var hotfixAsset = operation.AssetObject as TextAsset;
            System.Reflection.Assembly.Load(hotfixAsset.bytes);
            Debug.Log("[HotfixUpdater] 热更新DLL加载完成");
        }
    }
}
