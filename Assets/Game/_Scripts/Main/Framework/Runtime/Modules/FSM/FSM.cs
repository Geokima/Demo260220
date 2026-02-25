using System;
using System.Collections.Generic;

namespace Framework.Modules.FSM
{
    public class FSM<T>
    {
        private Dictionary<T, IState> _states = new Dictionary<T, IState>();
        private Dictionary<(T from, T to), List<ITransitionCondition<T>>> _transitionConditions = new Dictionary<(T, T), List<ITransitionCondition<T>>>();
        private IState _currentState;
        private T _currentStateId;

        public event Action<T, T> OnStateChanged;
        public event Action<T> OnStateEnter;
        public event Action<T> OnStateExit;

        public T CurrentState => _currentStateId;
        public bool IsInState(T stateId) => EqualityComparer<T>.Default.Equals(_currentStateId, stateId);

        public void RegisterState(T stateId, IState state) => _states[stateId] = state;

        public void RegisterTransitionCondition(T fromState, T toState, ITransitionCondition<T> condition)
        {
            var key = (fromState, toState);
            if (!_transitionConditions.TryGetValue(key, out var conditions))
            {
                conditions = new List<ITransitionCondition<T>>();
                _transitionConditions[key] = conditions;
            }
            conditions.Add(condition);
        }

        public bool CanChangeState(T toState)
        {
            if (!_states.ContainsKey(toState)) return false;
            var key = (_currentStateId, toState);
            if (_transitionConditions.TryGetValue(key, out var conditions))
            {
                foreach (var condition in conditions)
                    if (!condition.CanTransition(_currentStateId, toState)) return false;
            }
            return true;
        }

        public bool TryChangeState(T stateId)
        {
            if (!CanChangeState(stateId)) return false;
            ChangeState(stateId);
            return true;
        }

        public void ChangeState(T stateId)
        {
            if (!_states.TryGetValue(stateId, out var newState))
                throw new ArgumentException($"State {stateId} not registered");

            var previousState = _currentStateId;
            OnStateExit?.Invoke(previousState);
            _currentState?.OnExit();

            _currentState = newState;
            _currentStateId = stateId;

            OnStateEnter?.Invoke(stateId);
            OnStateChanged?.Invoke(previousState, stateId);
            _currentState.OnEnter();
        }

        public void Update() => _currentState?.OnUpdate();
        public void FixedUpdate() => _currentState?.OnFixedUpdate();

        public void Clear()
        {
            _currentState?.OnExit();
            _currentState = null;
            _states.Clear();
            _transitionConditions.Clear();
            OnStateChanged = null;
            OnStateEnter = null;
            OnStateExit = null;
        }
    }
}
