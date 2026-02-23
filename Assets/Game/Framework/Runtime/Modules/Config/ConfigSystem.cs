using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
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

        public async UniTask LoadConfigsFrom(IResLoader loader, string folder = "Configs")
        {
            try
            {
                if (loader == null) return;

                var configRowTypes = ScanConfigRowTypes();
                
                foreach (var type in configRowTypes)
                {
                    var fileName = GetConfigFileName(type);
                    var asset = await loader.LoadAsync<TextAsset>($"{folder}/{fileName}");
                    if (asset == null) continue;

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
            if (typeName.EndsWith("ConfigRow"))
            {
                var baseName = typeName.Substring(0, typeName.Length - "ConfigRow".Length);
                return $"cfg_{baseName.ToLower()}";
            }
            return $"cfg_{typeName.ToLower()}";
        }

        private void ParseAndRegister(string json, Type rowType)
        {
            var parseMethod = GetType().GetMethod("ParseJson", BindingFlags.NonPublic | BindingFlags.Instance);
            var generic = parseMethod.MakeGenericMethod(rowType);
            var configs = generic.Invoke(this, new[] { json }) as System.Collections.IList;

            if (configs == null || configs.Count == 0) return;

            var registerMethod = GetType().GetMethod("RegisterConfigs", BindingFlags.NonPublic | BindingFlags.Instance);
            var genericRegister = registerMethod.MakeGenericMethod(rowType);
            genericRegister.Invoke(this, new[] { configs });
        }

        private List<T> ParseJson<T>(string json) where T : IConfigRow
        {
            var wrapped = $"{{\"array\":{json}}}";
            var wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(wrapped);
            return wrapper?.array?.ToList() ?? new List<T>();
        }

        private void RegisterConfigs<T>(System.Collections.IList configs) where T : IConfigRow
        {
            var dict = new Dictionary<int, T>();
            foreach (T config in configs)
                dict[config.Id] = config;
            _sheets[typeof(T)] = new ConfigSheet<T>(dict);
        }

        [Serializable]
        private class JsonArrayWrapper<T>
        {
            public T[] array;
        }

        public override void Init() { }

        public override void Deinit() => _sheets.Clear();
    }
}
