namespace Framework.Modules.FSM
{
    /// <summary>
    /// 有限状态机状态接口
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// 进入状态时调用
        /// </summary>
        void OnEnter();

        /// <summary>
        /// 状态更新时调用
        /// </summary>
        void OnUpdate();

        /// <summary>
        /// 固定频率更新时调用
        /// </summary>
        void OnFixedUpdate();

        /// <summary>
        /// 退出状态时调用
        /// </summary>
        void OnExit();
    }
}
