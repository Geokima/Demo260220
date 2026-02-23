namespace Framework.Modules.Res
{
    using System;
    using Cysharp.Threading.Tasks;
    using UnityEngine;
    
    public class ResSystem : AbstractSystem
    {
        private IResLoader _resourceLoader;
        private IResLoader _assetLoader;

        public IResLoader ResourceLoader => _resourceLoader;
        public IResLoader AssetLoader => _assetLoader;

        public void UnloadUnusedAssets()
        {
            _resourceLoader?.UnloadUnusedAssets();
            _assetLoader?.UnloadUnusedAssets();
        }

        public override void Init()
        {
            _resourceLoader = new ResourcesLoader();
            _assetLoader = new YooAssetLoader();
            Application.backgroundLoadingPriority = ThreadPriority.High;
        }

        public override void Deinit()
        {
            _resourceLoader?.UnloadUnusedAssets();
            _assetLoader?.UnloadUnusedAssets();
            _resourceLoader = null;
            _assetLoader = null;
        }
    }
}