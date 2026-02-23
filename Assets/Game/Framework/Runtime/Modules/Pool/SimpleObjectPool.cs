using System;

namespace Framework.Modules.Pool
{
    public class SimpleObjectPool<T> : Pool<T> where T : new()
    {
        private readonly Action<T> _onReset;
        public SimpleObjectPool(Action<T> onReset = null) => _onReset = onReset;

        protected override T Create() => new T();

        public override bool Recycle(T obj)
        {
            _onReset?.Invoke(obj);
            return base.Recycle(obj);
        }
    }
}
