using System;
using System.Collections.Generic;
using Framework;
using Framework.Modules.Config;
using Game.Config;
using Game.Effect;
using Game.Player;
using Game.Procedures;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Effect
{
    public class EffectSystem: AbstractSystem
    {
        private Dictionary<int, EffectConfig> _configs = new();
        private Dictionary<string, Action<Dictionary<string, string>>> _handlers = new();

        public override void Init()
        {
            this.RegisterEvent<PreloadCompleteEvent>(OnConfigLoaded);
        }

        private void OnConfigLoaded(PreloadCompleteEvent @event)
        {
            var configSystem = this.GetSystem<IConfigSystem>();
            var sheet = configSystem.GetSheet<EffectConfig>();
            foreach (var row in sheet.All())
            {
                _configs[row.Id] = row;
            }

            Register("AddGold", (p) =>
            {
                Debug.Log($"[EffectSystem] <color=yellow>表现：飞金币特效</color>");
            });

            Register("AddEnergy", (p) =>
            {
                Debug.Log($"[EffectSystem] <color=orange>表现：心跳恢复特效</color>");
            });

            Register("AddExp", (p) =>
            {
                Debug.Log($"[EffectSystem] <color=cyan>表现：经验条上涨特效</color>");
            });

            Register("SpendEnergy", (p) =>
            {
                Debug.Log($"[EffectSystem] <color=red>表现：体力减少特效</color>");
            });

            Register("SpendGold", (p) =>
            {
                Debug.Log($"[EffectSystem] <color=red>表现：金币减少特效</color>");
            });
        }


        public void Register(string type, Action<Dictionary<string, string>> handler)
        {
            _handlers[type] = handler;
        }

        public void Execute(int effectId, Dictionary<string, string> serverParams)
        {
            if (!_configs.TryGetValue(effectId, out var config))
            {
                Debug.LogWarning($"[EffectSystem] Effect not found: {effectId}");
                return;
            }

            var configParams = JsonConvert.DeserializeObject<Dictionary<string, string>>(config.Params ?? "{}");
            if (serverParams != null)
            {
                foreach (var kv in serverParams)
                {
                    configParams[kv.Key] = kv.Value;
                }
            }

            if (_handlers.TryGetValue(config.Type, out var handler))
            {
                handler(configParams);
            }

            if (!string.IsNullOrEmpty(config.Vfx))
            {
                PlayVfx(config.Vfx);
            }
            
            if (!string.IsNullOrEmpty(config.Sfx))
            {
                PlaySfx(config.Sfx);
            }
        }

        private void PlayVfx(string vfx)
        {
            Debug.Log($"[EffectSystem] Play VFX: {vfx}");
        }

        private void PlaySfx(string sfx)
        {
            Debug.Log($"[EffectSystem] Play SFX: {sfx}");
        }
    }
}
