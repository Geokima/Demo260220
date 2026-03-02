namespace Framework.Modules.FSM
{
    /// <summary>
    /// 状态基类
    /// </summary>
    public abstract class StateBase : IState
    {
        /// <inheritdoc />
        public virtual void OnEnter() { }
        /// <inheritdoc />
        public virtual void OnUpdate() { }
        /// <inheritdoc />
        public virtual void OnFixedUpdate() { }
        /// <inheritdoc />
        public virtual void OnExit() { }
    }

    /// <summary>
    /// 带上下文的状态基类
    /// </summary>
    /// <typeparam name="TContext">上下文类型</typeparam>
    public abstract class StateBase<TContext> : IState
    {
        /// <summary>
        /// 状态上下文
        /// </summary>
        protected TContext Context { get; }

        protected StateBase(TContext context) => Context = context;

        /// <inheritdoc />
        public virtual void OnEnter() { }
        /// <inheritdoc />
        public virtual void OnUpdate() { }
        /// <inheritdoc />
        public virtual void OnFixedUpdate() { }
        /// <inheritdoc />
        public virtual void OnExit() { }
    }
}
