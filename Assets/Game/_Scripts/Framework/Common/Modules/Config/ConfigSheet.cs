using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Modules.Config
{
    /// <summary>
    /// 配置表单实现类
    /// </summary>
    /// <typeparam name="TRow">配置行类型</typeparam>
    public class ConfigSheet<TRow> : IConfigSheet<TRow> where TRow : IConfigRow
    {
        private readonly Dictionary<int, TRow> _data;

        public ConfigSheet(Dictionary<int, TRow> data) => _data = data;

        /// <inheritdoc />
        public int Count => _data.Count;

        /// <inheritdoc />
        public TRow Get(int id) => _data.TryGetValue(id, out var row) ? row : default;

        /// <inheritdoc />
        public bool TryGet(int id, out TRow row) => _data.TryGetValue(id, out row);

        /// <inheritdoc />
        public TRow FindBy<TField>(Func<TRow, TField> selector, TField value)
            => _data.Values.FirstOrDefault(r => EqualityComparer<TField>.Default.Equals(selector(r), value));

        /// <inheritdoc />
        public IEnumerable<TRow> Where(Func<TRow, bool> predicate)
            => _data.Values.Where(predicate);

        /// <inheritdoc />
        public Dictionary<TField, TRow> ToIndex<TField>(Func<TRow, TField> keySelector)
            => _data.Values.ToDictionary(keySelector);

        /// <inheritdoc />
        public IEnumerable<TRow> All() => _data.Values;
    }
}
