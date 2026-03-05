using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Framework;
using Framework.Modules.Config;
using Framework.Modules.Procedure;
using Framework.Modules.Res;
using UnityEngine;

namespace Game.Procedures
{
    public class PreloadProcedure : ProcedureBase
    {
        public override async void OnEnter()
        {
            await LoadConfigsAsync();
            ChangeProcedure<LoginProcedure>();
        }

        private async UniTask LoadConfigsAsync()
        {
            var configSystem = this.GetSystem<IConfigSystem>();
            var resSystem = this.GetSystem<IResSystem>();

            var configRowTypes = configSystem.ScanConfigRowTypes();
            var tasks = new List<UniTask>();

            foreach (var type in configRowTypes)
            {
                tasks.Add(LoadSingleConfig(configSystem, resSystem, type));
            }

            await UniTask.WhenAll(tasks);

            Debug.Log($"[Config] All {configRowTypes.Count} configs loaded.");
            Owner.SendEvent(new PreloadCompleteEvent());
        }

        private async UniTask LoadSingleConfig(IConfigSystem configSystem, IResSystem resSystem, Type rowType)
        {
            var fileName = configSystem.GetConfigFileName(rowType);

            if (!resSystem.Exists(fileName))
            {
                Debug.LogWarning($"[Config] Config file not found: {fileName}");
                return;
            }

            var asset = await resSystem.LoadAsync<TextAsset>(fileName);
            if (asset == null)
            {
                Debug.LogError($"[Config] Failed to load config asset: {fileName}");
                return;
            }

            configSystem.RegisterConfig(asset.text, rowType);
        }
    }

    public struct PreloadCompleteEvent : IEvent
    {
    }
}
