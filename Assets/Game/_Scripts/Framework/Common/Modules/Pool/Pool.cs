using System.Collections.Generic;

namespace Framework.Modules.Pool
{
    /// <summary>
    /// 对象池基类
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public abstract class Pool<T> : IPool<T>
    {
        protected readonly Stack<T> _cache = new Stack<T>();
        protected readonly HashSet<T> _inPoolCheck = new HashSet<T>();

        /// <inheritdoc />
        public int Count => _cache.Count;

        /// <summary>
        /// 创建新对象
        /// </summary>
        /// <returns>新创建的对象</returns>
        protected abstract T Create();

        /// <inheritdoc />
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

        /// <inheritdoc />
        public virtual bool Recycle(T obj)
        {
            if (obj == null || _inPoolCheck.Contains(obj)) return false; 
            
            if (obj is IPoolObject poolObj) poolObj.OnDespawn();
            _cache.Push(obj);
            _inPoolCheck.Add(obj);
            return true;
        }

        /// <summary>
        /// 清空池
        /// </summary>
        public virtual void Clear()
        {
            _cache.Clear();
            _inPoolCheck.Clear();
        }
    }
}
