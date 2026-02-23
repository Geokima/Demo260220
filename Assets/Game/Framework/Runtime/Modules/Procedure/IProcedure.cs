using Framework.Modules.FSM;

namespace Framework.Modules.Procedure
{
    public interface IProcedure : IState, IBelongToArchitecture
    {
        ProcedureType Type { get; }
        void OnInit();
        void OnShutdown();
    }
}
