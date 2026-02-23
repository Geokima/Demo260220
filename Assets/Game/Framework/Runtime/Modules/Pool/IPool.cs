namespace Framework.Modules.Pool
{
    public interface IPool
    {
        int Count { get; }
    }

    public interface IPool<T> : IPool
    {
        T Allocate();
        bool Recycle(T obj);
    }

    public interface IPoolObject
    {
        void OnSpawn();
        void OnDespawn();
    }
}
