using System;

namespace Framework.Modules.FSM
{
    public interface ITransitionCondition<T>
    {
        bool CanTransition(T fromState, T toState);
    }

    public class TransitionCondition<T> : ITransitionCondition<T>
    {
        private readonly Func<T, T, bool> _condition;

        public TransitionCondition(Func<T, T, bool> condition) => _condition = condition;

        public bool CanTransition(T fromState, T toState) => _condition?.Invoke(fromState, toState) ?? true;
    }
}
