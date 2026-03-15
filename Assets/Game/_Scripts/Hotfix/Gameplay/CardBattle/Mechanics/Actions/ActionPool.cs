using Framework.Modules.Pool;

namespace Game.Gameplay.CardBattle
{
    public interface IPoolableAction : IBattleAction
    {
        void Reset();
    }

    /// <summary> 提供统一的高性能操作对象池。消除实例化造成的 GC Alloc。 </summary>
    public static class ActionPool<T> where T : class, IPoolableAction, new()
    {
        private static SimpleObjectPool<T> _pool = new SimpleObjectPool<T>(a => a.Reset());
        
        public static T Allocate() => _pool.Allocate();
        public static void Recycle(T action) => _pool.Recycle(action);
    }
}
