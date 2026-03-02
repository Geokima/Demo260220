using System;
using System.Collections.Generic;

namespace Framework.Modules.FSM
{
    /// <summary>
    /// 有限状态机
    /// </summary>
    /// <typeparam name="T">状态标识类型</typeparam>
    public class FSM<T>
    {
        private readonly Dictionary<T, IState> _states = new Dictionary<T, IState>();
        private readonly Dictionary<(T from, T to), List<ITransitionCondition<T>>> _transitionConditions = new Dictionary<(T, T), List<ITransitionCondition<T>>>();
        private IState _currentState;
        private T _currentStateId;

        /// <summary>
        /// 状态切换事件：参数1为原状态，参数2为新状态
        /// </summary>
        public event Action<T, T> OnStateChanged;

        /// <summary>
        /// 进入状态事件
        /// </summary>
        public event Action<T> OnStateEnter;

        /// <summary>
        /// 退出状态事件
        /// </summary>
        public event Action<T> OnStateExit;

        /// <summary>
        /// 当前状态标识
        /// </summary>
        public T CurrentState => _currentStateId;

        /// <summary>
        /// 检查是否处于指定状态
        /// </summary>
        /// <param name="stateId">状态标识</param>
        /// <returns>是否处于该状态</returns>
        public bool IsInState(T stateId) => EqualityComparer<T>.Default.Equals(_currentStateId, stateId);

        /// <summary>
        /// 注册状态
        /// </summary>
        /// <param name="stateId">状态标识</param>
        /// <param name="state">状态实例</param>
        public void RegisterState(T stateId, IState state) => _states[stateId] = state;

        /// <summary>
        /// 注册状态转换条件
        /// </summary>
        /// <param name="fromState">起始状态</param>
        /// <param name="toState">目标状态</param>
        /// <param name="condition">转换条件</param>
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

        /// <summary>
        /// 检查是否可以切换到指定状态
        /// </summary>
        /// <param name="toState">目标状态</param>
        /// <returns>是否可以切换</returns>
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

        /// <summary>
        /// 尝试切换状态
        /// </summary>
        /// <param name="stateId">目标状态标识</param>
        /// <returns>是否切换成功</returns>
        public bool TryChangeState(T stateId)
        {
            if (!CanChangeState(stateId)) return false;
            ChangeState(stateId);
            return true;
        }

        /// <summary>
        /// 强制切换状态
        /// </summary>
        /// <param name="stateId">目标状态标识</param>
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

        /// <summary>
        /// 每帧更新
        /// </summary>
        public void Update() => _currentState?.OnUpdate();

        /// <summary>
        /// 固定更新
        /// </summary>
        public void FixedUpdate() => _currentState?.OnFixedUpdate();

        /// <summary>
        /// 清理 FSM
        /// </summary>
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
