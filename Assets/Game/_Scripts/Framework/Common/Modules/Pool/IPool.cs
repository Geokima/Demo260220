namespace Framework.Modules.Pool
{
    /// <summary>
    /// 对象池基础接口
    /// </summary>
    public interface IPool
    {
        /// <summary>
        /// 池中当前缓存的对象数量
        /// </summary>
        int Count { get; }
    }

    /// <summary>
    /// 泛型对象池接口
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    public interface IPool<T> : IPool
    {
        /// <summary>
        /// 从池中分配（获取）一个对象
        /// </summary>
        /// <returns>分配的对象</returns>
        T Allocate();

        /// <summary>
        /// 将对象回收至池中
        /// </summary>
        /// <param name="obj">要回收的对象</param>
        /// <returns>回收是否成功</returns>
        bool Recycle(T obj);
    }
}
