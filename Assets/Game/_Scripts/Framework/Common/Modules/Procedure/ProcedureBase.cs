using Framework;

namespace Framework.Modules.Procedure
{
    /// <summary>
    /// 流程基类
    /// </summary>
    public abstract class ProcedureBase : IProcedure
    {
        /// <inheritdoc />
        public IArchitecture Architecture { get; set; } = default!;

        /// <summary>
        /// 所属流程系统
        /// </summary>
        public ProcedureSystem Owner { get; set; } = default!;

        /// <inheritdoc />
        public virtual void OnInit() { }
        /// <inheritdoc />
        public virtual void OnEnter() { }
        /// <inheritdoc />
        public virtual void OnUpdate() { }
        /// <inheritdoc />
        public virtual void OnFixedUpdate() { }
        /// <inheritdoc />
        public virtual void OnExit() { }
        /// <inheritdoc />
        public virtual void OnShutdown() { }

        /// <summary>
        /// 切换流程
        /// </summary>
        /// <typeparam name="T">目标流程类型</typeparam>
        protected void ChangeProcedure<T>() where T : IProcedure
        {
            Owner.ChangeProcedure<T>();
        }
    }
}
