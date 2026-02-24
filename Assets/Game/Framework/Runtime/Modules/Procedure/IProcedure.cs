using Framework.Modules.FSM;

namespace Framework.Modules.Procedure
{
    public interface IProcedure : IState, IBelongToArchitecture
    {
        void OnInit();
        void OnShutdown();
    }
}
