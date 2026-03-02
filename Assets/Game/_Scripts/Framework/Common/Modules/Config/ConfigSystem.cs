using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Framework;
using static Framework.Logger;

namespace Framework.Modules.Config
{
    /// <summary>
    /// 配置系统实现类
    /// </summary>
    public class ConfigSystem : AbstractSystem, IConfigSystem
    {
        #region Fields

        private readonly Dictionary<Type, object> _sheets = new Dictionary<Type, object>();

        #endregion

        #region Lifecycle

        /// <inheritdoc />
        public override void Init()
        {
        }

        /// <inheritdoc />
        public override void Deinit()
        {
            _sheets.Clear();
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public IConfigSheet<TRow> GetSheet<TRow>() where TRow : IConfigRow
        {
            if (_sheets.TryGetValue(typeof(TRow), out var sheet))
                return sheet as IConfigSheet<TRow>;

            return new ConfigSheet<TRow>(new Dictionary<int, TRow>());
        }

        /// <inheritdoc />
        public TRow Get<TRow>(int id) where TRow : class, IConfigRow
            => GetSheet<TRow>()?.Get(id) ?? default;

        /// <inheritdoc />
        public List<Type> ScanConfigRowTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(IConfigRow).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .ToList();
        }

        /// <inheritdoc />
        public string GetConfigFileName(Type configRowType)
        {
            var typeName = configRowType.Name;
            if (typeName.EndsWith("Config"))
            {
                var baseName = typeName.Substring(0, typeName.Length - "Config".Length);
                return $"cfg_{baseName.ToLower()}";
            }
            return $"cfg_{typeName.ToLower()}";
        }

        /// <inheritdoc />
        public void RegisterConfig(string json, Type rowType)
        {
            if (!IsValidJson(json))
            {
                LogError($"[Config] Invalid JSON format for type: {rowType.Name}");
                return;
            }

            var jArray = JArray.Parse(json);
            var configs = new List<object>();

            bool hasAnyId = jArray.Any(j => j["Id"] != null);
            if (!hasAnyId)
            {
                LogWarning($"[Config] No Id field found in {rowType.Name}, will auto assign");
            }

            int autoIndex = 1;
            foreach (var jToken in jArray)
            {
                var jObject = (JObject)jToken;
                if (jObject["Id"] == null)
                    jObject["Id"] = autoIndex;

                var config = jObject.ToObject(rowType);
                configs.Add(config);
                autoIndex++;
            }

            if (configs.Count == 0)
            {
                LogWarning($"[Config] No configs found for type: {rowType.Name}");
                return;
            }

            var registerMethod = GetType().GetMethod("RegisterConfigsInternal", BindingFlags.NonPublic | BindingFlags.Instance);
            var genericRegister = registerMethod.MakeGenericMethod(rowType);
            genericRegister.Invoke(this, new[] { configs });

            Log($"[Config] Registered {configs.Count} configs for type: {rowType.Name}");
        }

        #endregion

        #region Private Methods

        private bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            json = json.Trim();
            return (json.StartsWith("[") && json.EndsWith("]")) || (json.StartsWith("{") && json.EndsWith("}"));
        }

        /// <summary>
        /// 内部注册配置表，由反射调用
        /// </summary>
        private void RegisterConfigsInternal<T>(System.Collections.IList configs) where T : IConfigRow
        {
            var dict = new Dictionary<int, T>();
            foreach (T config in configs)
                dict[config.Id] = config;
            _sheets[typeof(T)] = new ConfigSheet<T>(dict);
        }

        #endregion
    }
}
