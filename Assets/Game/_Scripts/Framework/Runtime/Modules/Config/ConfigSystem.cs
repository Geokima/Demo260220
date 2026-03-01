using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Framework.Modules.Config
{
    using Res;

    public class ConfigSystem : AbstractSystem
    {
        private readonly Dictionary<Type, object> _sheets = new Dictionary<Type, object>();

        public IConfigSheet<TRow> GetSheet<TRow>() where TRow : IConfigRow
        {
            if (_sheets.TryGetValue(typeof(TRow), out var sheet))
                return sheet as IConfigSheet<TRow>;

            return new ConfigSheet<TRow>(new Dictionary<int, TRow>());
        }

        public TRow Get<TRow>(int id) where TRow : class, IConfigRow
            => GetSheet<TRow>()?.Get(id) ?? default;

        public async UniTask LoadConfigsFrom(IResLoader loader)
        {
            try
            {
                if (loader == null) return;

                var configRowTypes = ScanConfigRowTypes();

                foreach (var type in configRowTypes)
                {
                    var fileName = GetConfigFileName(type);

                    if (!loader.Exists(fileName))
                    {
                        Debug.LogWarning($"[Config] Config file not found: {fileName}");
                        continue;
                    }

                    var asset = await loader.LoadAsync<TextAsset>(fileName);
                    if (asset == null)
                    {
                        Debug.LogError($"[Config] Failed to load config: {fileName}");
                        continue;
                    }

                    ParseAndRegister(asset.text, type);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Config] LoadAll failed: {ex.Message}");
            }
        }

        private List<Type> ScanConfigRowTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IConfigRow).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .ToList();
        }

        private string GetConfigFileName(Type configRowType)
        {
            var typeName = configRowType.Name;
            if (typeName.EndsWith("Config"))
            {
                var baseName = typeName.Substring(0, typeName.Length - "Config".Length);
                return $"cfg_{baseName.ToLower()}";
            }
            return $"cfg_{typeName.ToLower()}";
        }

        private void ParseAndRegister(string json, Type rowType)
        {
            if (!IsValidJson(json))
            {
                Debug.LogError($"[Config] Invalid JSON format for type: {rowType.Name}");
                return;
            }

            var jArray = JArray.Parse(json);
            var configs = new List<object>();

            bool hasAnyId = jArray.Any(j => j["Id"] != null);
            if (!hasAnyId)
            {
                Debug.LogWarning($"[Config] No Id field found in {rowType.Name}, will auto assign");
            }

            var existingIds = new HashSet<int>();
            bool hasDuplicate = false;

            int autoIndex = 1;
            foreach (var jToken in jArray)
            {
                var jObject = (JObject)jToken;

                if (jObject["Id"] == null)
                {
                    jObject["Id"] = autoIndex;
                }

                int id = jObject["Id"].Value<int>();

                if (!existingIds.Add(id))
                {
                    Debug.LogWarning($"[Config] Duplicate Id {id} found in {rowType.Name}");
                    hasDuplicate = true;
                }

                var config = jObject.ToObject(rowType);
                configs.Add(config);
                autoIndex++;
            }

            if (configs.Count == 0)
            {
                Debug.LogWarning($"[Config] No configs found for type: {rowType.Name}");
                return;
            }

            var registerMethod = GetType().GetMethod("RegisterConfigs", BindingFlags.NonPublic | BindingFlags.Instance);
            var genericRegister = registerMethod.MakeGenericMethod(rowType);
            genericRegister.Invoke(this, new[] { configs });

            var idSource = hasAnyId ? "json" : "auto";
            Debug.Log($"[Config] Registered {configs.Count} configs for type: {rowType.Name} (Id source: {idSource}, duplicate: {hasDuplicate})");
        }

        private bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            json = json.Trim();
            if ((json.StartsWith("[") && json.EndsWith("]")) ||
                (json.StartsWith("{") && json.EndsWith("}")))
            {
                try
                {
                    JsonConvert.DeserializeObject(json);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        // 注意：此方法通过反射在 ParseAndRegister 中调用
        private void RegisterConfigs<T>(System.Collections.IList configs) where T : IConfigRow
        {
            var dict = new Dictionary<int, T>();
            foreach (T config in configs)
                dict[config.Id] = config;
            _sheets[typeof(T)] = new ConfigSheet<T>(dict);
        }



        public override void Init() { }

        public override void Deinit() => _sheets.Clear();
    }
}
