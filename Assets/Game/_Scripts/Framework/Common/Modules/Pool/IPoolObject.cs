namespace Framework.Modules.Pool
{
    /// <summary>
    /// 池对象接口，实现此接口的对象在出池和入池时会收到通知
    /// </summary>
    public interface IPoolObject
    {
        /// <summary>
        /// 对象被分配（生成）时调用
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// 对象被回收（销毁）时调用
        /// </summary>
        void OnDespawn();
    }
}
