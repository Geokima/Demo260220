using System.Collections.Generic;

namespace Framework.Modules.Pool
{
    public abstract class Pool<T> : IPool<T>
    {
        protected readonly Stack<T> _cache = new Stack<T>();
        protected readonly HashSet<T> _inPoolCheck = new HashSet<T>(); 
        public int Count => _cache.Count;

        protected abstract T Create();

        public virtual T Allocate()
        {
            T obj = default!;
            while (_cache.Count > 0)
            {
                obj = _cache.Pop();
                if (obj != null) break;
            }

            if (obj == null) obj = Create();
            
            if (obj == null) return default!;
            
            _inPoolCheck.Remove(obj);
            if (obj is IPoolObject poolObj) poolObj.OnSpawn();
            return obj;
        }

        public virtual bool Recycle(T obj)
        {
            if (obj == null || _inPoolCheck.Contains(obj)) return false; 
            
            if (obj is IPoolObject poolObj) poolObj.OnDespawn();
            _cache.Push(obj);
            _inPoolCheck.Add(obj);
            return true;
        }

        public virtual void Clear()
        {
            _cache.Clear();
            _inPoolCheck.Clear();
        }
    }
}
