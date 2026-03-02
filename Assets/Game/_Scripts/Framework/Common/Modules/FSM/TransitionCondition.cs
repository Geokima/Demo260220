using System;

namespace Framework.Modules.FSM
{
    /// <summary>
    /// 状态转换条件实现类
    /// </summary>
    /// <typeparam name="T">状态标识类型</typeparam>
    public class TransitionCondition<T> : ITransitionCondition<T>
    {
        private readonly Func<T, T, bool> _condition;

        public TransitionCondition(Func<T, T, bool> condition) => _condition = condition;

        /// <inheritdoc />
        public bool CanTransition(T fromState, T toState) => _condition?.Invoke(fromState, toState) ?? true;
    }
}
