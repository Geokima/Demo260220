namespace Framework.Modules.FSM
{
    public interface IState
    {
        void OnEnter();
        void OnUpdate();
        void OnFixedUpdate();
        void OnExit();
    }

    public abstract class StateBase : IState
    {
        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnExit() { }
    }

    public abstract class StateBase<TContext> : IState
    {
        protected TContext Context { get; }

        protected StateBase(TContext context) => Context = context;

        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnExit() { }
    }
}
