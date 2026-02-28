namespace Framework.Modules.Procedure
{
    public abstract class ProcedureBase : IProcedure
    {
        public IArchitecture Architecture { get; set; } = default!;
        public ProcedureSystem Owner { get; set; } = default!;

        public virtual void OnInit() { }
        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnExit() { }
        public virtual void OnShutdown() { }

        protected void ChangeProcedure<T>() where T : IProcedure
        {
            Owner.ChangeProcedure<T>();
        }
    }
}
