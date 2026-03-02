using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Game.Main
{
    public static class LauncherAssets
    {
        public static ResourcePackage DefaultPackage { get; private set; }

        public static async UniTask InitAsync(EPlayMode playMode, string packageName, Action<float, string> onProgress)
        {
            Debug.Log($"[LauncherAssets] 开始初始化资源包: {packageName}，模式: {playMode}");
            onProgress?.Invoke(0f, "正在初始化资源系统...");

            YooAssets.Initialize();
            DefaultPackage = YooAssets.CreatePackage(packageName);
            YooAssets.SetDefaultPackage(DefaultPackage);

            var op = CreateInitOperation(playMode, packageName);
            await op;

            if (op.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[LauncherAssets] 资源包初始化失败: {op.Error}");
                throw new Exception($"资源包初始化失败: {op.Error}");
            }

            Debug.Log("[LauncherAssets] 资源包初始化成功");
            onProgress?.Invoke(0.1f, "资源系统初始化完成");
        }

        public static async UniTask UpdateAsync(EPlayMode playMode, Action<float, string> onProgress)
        {
            Debug.Log("[LauncherAssets] 开始检查版本和下载");

            onProgress?.Invoke(0.15f, "正在检查服务器版本...");
            var versionOp = DefaultPackage.RequestPackageVersionAsync();
            await versionOp;
            if (versionOp.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[LauncherAssets] 请求版本号失败: {versionOp.Error}");
                throw new Exception($"检查版本失败: {versionOp.Error}");
            }

            onProgress?.Invoke(0.2f, "正在更新资源清单...");
            var manifestOp = DefaultPackage.UpdatePackageManifestAsync(versionOp.PackageVersion);
            await manifestOp;
            if (manifestOp.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[LauncherAssets] 更新资源清单失败: {manifestOp.Error}");
                throw new Exception($"更新清单失败: {manifestOp.Error}");
            }

            var downloader = DefaultPackage.CreateResourceDownloader(10, 3);
            if (downloader.TotalDownloadCount == 0)
            {
                Debug.Log("[LauncherAssets] 资源已是最新");
                onProgress?.Invoke(0.9f, "资源已是最新");
                return;
            }

            long totalBytes = downloader.TotalDownloadBytes;
            if (GetAvailableDiskSpace() < totalBytes * 1.5f)
            {
                Debug.LogError("[LauncherAssets] 磁盘空间不足！");
                throw new Exception($"磁盘空间不足，需要 {totalBytes / 1024 / 1024}MB");
            }

            downloader.DownloadUpdateCallback = (info) =>
            {
                float progress = Mathf.Lerp(0.2f, 0.9f, (float)info.CurrentDownloadBytes / totalBytes);
                string mb = (info.CurrentDownloadBytes / 1024f / 1024f).ToString("F1");
                string totalMb = (totalBytes / 1024f / 1024f).ToString("F1");
                onProgress?.Invoke(progress, $"正在下载更新资源 ({mb}MB/{totalMb}MB)...");
            };

            downloader.BeginDownload();
            await downloader;

            if (downloader.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[LauncherAssets] 资源下载失败: {downloader.Error}");
                throw new Exception($"资源下载失败: {downloader.Error}");
            }

            Debug.Log("[LauncherAssets] 资源下载并更新完成");
            onProgress?.Invoke(0.9f, "资源更新完成");
        }

        private static InitializationOperation CreateInitOperation(EPlayMode playMode, string packageName)
        {
            if (playMode == EPlayMode.EditorSimulateMode)
            {
                var result = EditorSimulateModeHelper.SimulateBuild(packageName);
                return DefaultPackage.InitializeAsync(new EditorSimulateModeParameters
                {
                    EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(result.PackageRootDirectory)
                });
            }

            if (playMode == EPlayMode.OfflinePlayMode)
            {
                return DefaultPackage.InitializeAsync(new OfflinePlayModeParameters
                {
                    BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                });
            }

            var host = GetHostServerURL();
            var remote = new RemoteServices(host, host);
            return DefaultPackage.InitializeAsync(new HostPlayModeParameters
            {
                BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(),
                CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remote)
            });
        }

        private static string GetHostServerURL()
        {
#if UNITY_EDITOR
            return "http://127.0.0.1:8080";
#else
            throw new NotImplementedException("HostPlayMode需要配置CDN地址");
#endif
        }

        private static long GetAvailableDiskSpace()
        {
            var drive = new DriveInfo(Path.GetPathRoot(Application.persistentDataPath));
            return drive.AvailableFreeSpace;
        }

        private class RemoteServices : IRemoteServices
        {
            private readonly string _defaultHost;
            private readonly string _fallbackHost;
            public RemoteServices(string defaultHost, string fallbackHost)
            {
                _defaultHost = defaultHost;
                _fallbackHost = fallbackHost;
            }
            public string GetRemoteMainURL(string fileName) => $"{_defaultHost}/{fileName}";
            public string GetRemoteFallbackURL(string fileName) => $"{_fallbackHost}/{fileName}";
        }
    }
}
