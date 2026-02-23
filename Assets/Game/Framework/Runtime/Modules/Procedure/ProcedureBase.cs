namespace Framework.Modules.Procedure
{
    public abstract class ProcedureBase : IProcedure
    {
        public IArchitecture Architecture { get; set; } = default!;
        public abstract ProcedureType Type { get; }

        public virtual void OnInit() { }
        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }
        public virtual void OnExit() { }
        public virtual void OnShutdown() { }

        protected void ChangeProcedure(ProcedureType type)
        {
            Architecture.GetSystem<ProcedureSystem>().ChangeProcedure(type);
        }
    }
}
