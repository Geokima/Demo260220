using Framework.Modules.FSM;
using Framework;

namespace Framework.Modules.Procedure
{
    /// <summary>
    /// 流程接口
    /// </summary>
    public interface IProcedure : IState, IController
    {
        /// <summary>
        /// 流程初始化时调用
        /// </summary>
        void OnInit();

        /// <summary>
        /// 流程销毁时调用
        /// </summary>
        void OnShutdown();
    }
}
