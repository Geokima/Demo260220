namespace Framework.Modules.FSM
{
    /// <summary>
    /// 状态转换条件接口
    /// </summary>
    /// <typeparam name="T">状态标识类型</typeparam>
    public interface ITransitionCondition<T>
    {
        /// <summary>
        /// 是否允许从 fromState 转换到 toState
        /// </summary>
        /// <param name="fromState">当前状态标识</param>
        /// <param name="toState">目标状态标识</param>
        /// <returns>是否允许转换</returns>
        bool CanTransition(T fromState, T toState);
    }
}
