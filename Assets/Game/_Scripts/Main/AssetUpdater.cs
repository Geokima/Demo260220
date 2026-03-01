using UnityEngine;
using YooAsset;
using System.IO;
using System;
using Cysharp.Threading.Tasks;

namespace Game.Main
{
    public class AssetUpdater
    {
        private static AssetUpdater _instance;
        public static AssetUpdater Instance => _instance ??= new AssetUpdater();
        public ResourcePackage DefaultPackage => _package;
        private EPlayMode _playMode;
        private ResourcePackage _package;

        public event Action OnInitStart;
        public event Action OnInitSuccess;
        public event Action<string> OnInitFailed; // 错误信息
        public event Action<long, long> OnDownloadProgress; // 总大小, 已下载
        public event Action OnDownloadSuccess;
        public event Action<string> OnDownloadFailed; // 错误信息
        public event Action<long, long> OnDiskSpaceNotEnough; // 可用空间, 需要空间

        private AssetUpdater() { }

        public async UniTask CheckVersionAndDownloadAsync(EPlayMode playMode, string packageName)
        {
            Debug.Log("[AssetUpdater] 开始检查版本");
            _playMode = playMode;

            Debug.Log("[AssetUpdater] 初始化资源系统");
            YooAssets.Initialize();
            _package = YooAssets.CreatePackage(packageName);
            YooAssets.SetDefaultPackage(_package);

            OnInitStart?.Invoke();

            MainEntry.Instance.ActualProgress = 0;
            Debug.Log("[AssetUpdater] 初始化资源包");
            if (!await InitializePackageAsync()) return;
            MainEntry.Instance.ActualProgress = 0.2f;
            Debug.Log("[AssetUpdater] 请求版本号");
            if (!await RequestVersionAsync()) return;
            MainEntry.Instance.ActualProgress = 0.4f;
            Debug.Log("[AssetUpdater] 更新资源清单");
            if (!await UpdateManifestAsync()) return;
            MainEntry.Instance.ActualProgress = 0.6f;
            Debug.Log("[AssetUpdater] 下载资源");
            if (!await DownloadAssetsAsync()) return;
            MainEntry.Instance.ActualProgress = 0.8f;
            Debug.Log("[AssetUpdater] 检查版本完成");
            OnInitSuccess?.Invoke();
        }

        private async UniTask<bool> InitializePackageAsync()
        {
            InitializationOperation op = CreateInitOperation();
            await op;

            if (op.Status != EOperationStatus.Succeed)
            {
                OnInitFailed?.Invoke($"资源包初始化失败: {op.Error}");
                return false;
            }
            return true;
        }

        private InitializationOperation CreateInitOperation()
        {
            if (_playMode == EPlayMode.EditorSimulateMode)
            {
                var result = EditorSimulateModeHelper.SimulateBuild(_package.PackageName);
                var param = new EditorSimulateModeParameters
                {
                    EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(result.PackageRootDirectory)
                };
                return _package.InitializeAsync(param);
            }

            if (_playMode == EPlayMode.OfflinePlayMode)
            {
                var param = new OfflinePlayModeParameters
                {
                    BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                };
                return _package.InitializeAsync(param);
            }

            var host = GetHostServerURL();
            var remote = new RemoteServices(host, host);
            var hostParam = new HostPlayModeParameters
            {
                BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(),
                CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remote)
            };
            return _package.InitializeAsync(hostParam);
        }

        private async UniTask<bool> RequestVersionAsync()
        {
            var op = _package.RequestPackageVersionAsync();
            await op;

            if (op.Status != EOperationStatus.Succeed)
            {
                OnInitFailed?.Invoke($"检查版本失败: {op.Error}");
                return false;
            }
            return true;
        }

        private async UniTask<bool> UpdateManifestAsync()
        {
            var versionOp = _package.RequestPackageVersionAsync();
            await versionOp;

            var op = _package.UpdatePackageManifestAsync(versionOp.PackageVersion);
            await op;

            if (op.Status != EOperationStatus.Succeed)
            {
                OnInitFailed?.Invoke($"更新清单失败: {op.Error}");
                return false;
            }
            return true;
        }

        private async UniTask<bool> DownloadAssetsAsync()
        {
#if UNITY_EDITOR
            if (_playMode == EPlayMode.EditorSimulateMode)
            {
                return true;
            }
#endif
            var downloader = _package.CreateResourceDownloader(10, 3);

            if (downloader.TotalDownloadCount == 0)
            {
                return true;
            }

            long totalBytes = downloader.TotalDownloadBytes;
            long availableBytes = GetAvailableDiskSpace();

            if (availableBytes < totalBytes * 1.5f)
            {
                OnDiskSpaceNotEnough?.Invoke(availableBytes, totalBytes);
                return false;
            }

            downloader.DownloadUpdateCallback = (info) =>
            {
                OnDownloadProgress?.Invoke(totalBytes, info.TotalDownloadBytes);
            };

            downloader.BeginDownload();
            await downloader;

            if (downloader.Status == EOperationStatus.Succeed)
            {
                OnDownloadSuccess?.Invoke();
                return true;
            }
            else
            {
                OnDownloadFailed?.Invoke($"资源下载失败: {downloader.Error}");
                return false;
            }
        }

        private string GetHostServerURL()
        {
#if UNITY_EDITOR
            return "http://127.0.0.1:8080";
#else
            throw new NotImplementedException("HostPlayMode需要配置CDN地址");
#endif
        }

        private long GetAvailableDiskSpace()
        {
            DriveInfo drive = new DriveInfo(Path.GetPathRoot(Application.persistentDataPath));
            return drive.AvailableFreeSpace;
        }
    }

    public class RemoteServices : IRemoteServices
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
