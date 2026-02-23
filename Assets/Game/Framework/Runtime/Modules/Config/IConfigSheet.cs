using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Modules.Config
{
    public interface IConfigRow
    {
        int Id { get; }
    }

    public interface IConfigSheet<TRow> where TRow : IConfigRow
    {
        TRow Get(int id);
        bool TryGet(int id, out TRow row);

        TRow FindBy<TField>(Func<TRow, TField> selector, TField value);

        IEnumerable<TRow> Where(Func<TRow, bool> predicate);

        Dictionary<TField, TRow> ToIndex<TField>(Func<TRow, TField> keySelector);

        IEnumerable<TRow> All();
        int Count { get; }
    }

    public class ConfigSheet<TRow> : IConfigSheet<TRow> where TRow : IConfigRow
    {
        private readonly Dictionary<int, TRow> _data;

        public ConfigSheet(Dictionary<int, TRow> data) => _data = data;

        public TRow Get(int id) => _data.TryGetValue(id, out var row) ? row : default;
        public bool TryGet(int id, out TRow row) => _data.TryGetValue(id, out row);

        public TRow FindBy<TField>(Func<TRow, TField> selector, TField value)
            => _data.Values.FirstOrDefault(r => EqualityComparer<TField>.Default.Equals(selector(r), value));

        public IEnumerable<TRow> Where(Func<TRow, bool> predicate)
            => _data.Values.Where(predicate);

        public Dictionary<TField, TRow> ToIndex<TField>(Func<TRow, TField> keySelector)
            => _data.Values.ToDictionary(keySelector);

        public IEnumerable<TRow> All() => _data.Values;
        public int Count => _data.Count;
    }
}
