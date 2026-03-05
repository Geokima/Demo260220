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
                if (int.TryParse(p.GetValueOrDefault("gold", "0"), out var gold) && gold > 0)
                {
                    var playerModel = this.GetModel<PlayerModel>();
                    playerModel.Gold.Value += gold;
                    Debug.Log($"[EffectSystem] 增加金币: {gold}, 当前金币: {playerModel.Gold.Value}");
                }
            });

            Register("AddEnergy", (p) =>
            {
                if (int.TryParse(p.GetValueOrDefault("energy", "0"), out var energy) && energy > 0)
                {
                    var playerModel = this.GetModel<PlayerModel>();
                    int maxEnergy = playerModel.GetMaxEnergy();
                    playerModel.Energy.Value = Mathf.Min(playerModel.Energy.Value + energy, maxEnergy);
                    Debug.Log($"[EffectSystem] 恢复体力: {energy}, 当前体力: {playerModel.Energy.Value}/{maxEnergy}");
                }
            });

            Register("SpendEnergy", (p) =>
            {
                if (int.TryParse(p.GetValueOrDefault("energy", "0"), out var energy) && energy > 0)
                {
                    var playerModel = this.GetModel<PlayerModel>();
                    playerModel.Energy.Value = Mathf.Max(playerModel.Energy.Value - energy, 0);
                    Debug.Log($"[EffectSystem] 消耗体力: {energy}, 当前体力: {playerModel.Energy.Value}");
                }
            });

            Register("SpendGold", (p) =>
            {
                if (int.TryParse(p.GetValueOrDefault("gold", "0"), out var gold) && gold > 0)
                {
                    var playerModel = this.GetModel<PlayerModel>();
                    playerModel.Gold.Value = Mathf.Max(playerModel.Gold.Value - gold, 0);
                    Debug.Log($"[EffectSystem] 消耗金币: {gold}, 当前金币: {playerModel.Gold.Value}");
                }
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
            foreach (var kv in serverParams)
            {
                configParams[kv.Key] = kv.Value;
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
