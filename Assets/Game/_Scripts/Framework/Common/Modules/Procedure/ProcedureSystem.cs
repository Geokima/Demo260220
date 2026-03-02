using System;
using System.Collections.Generic;
using Framework;
using Framework.Modules.FSM;
using static Framework.Logger;

namespace Framework.Modules.Procedure
{
    /// <summary>
    /// 流程系统实现类
    /// </summary>
    public class ProcedureSystem : AbstractSystem, IProcedureSystem, IUpdateable, IFixedUpdateable
    {
        #region Fields

        private FSM<Type> _fsm;
        private bool _isStarted;
        private readonly List<IProcedure> _procedures = new List<IProcedure>();

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public Type CurrentProcedure => _fsm?.CurrentState;

        /// <inheritdoc />
        public bool IsInProcedure<T>() where T : IProcedure => _fsm?.IsInState(typeof(T)) ?? false;

        /// <inheritdoc />
        public event Action<Type, Type> OnProcedureChanged
        {
            add { if (_fsm != null) _fsm.OnStateChanged += value; }
            remove { if (_fsm != null) _fsm.OnStateChanged -= value; }
        }

        #endregion

        #region Lifecycle

        /// <inheritdoc />
        public override void Init()
        {
            _fsm = new FSM<Type>();
            _fsm.OnStateChanged += (from, to) =>
            {
                Log($"[Procedure] Transition: {from?.Name} -> {to?.Name}");
            };
        }

        /// <inheritdoc />
        public void OnUpdate() => _fsm.Update();

        /// <inheritdoc />
        public void OnFixedUpdate() => _fsm.FixedUpdate();

        /// <inheritdoc />
        public override void Deinit()
        {
            foreach (var procedure in _procedures)
            {
                procedure.OnShutdown();
            }
            _procedures.Clear();

            _fsm?.Clear();
            _fsm = null;
            _isStarted = false;
        }

        #endregion

        #region Interface Implementation

        /// <inheritdoc />
        public void RegisterProcedure(IProcedure procedure)
        {
            procedure.Architecture = Architecture;
            if (procedure is ProcedureBase procedureBase)
                procedureBase.Owner = this;

            procedure.OnInit();
            _fsm.RegisterState(procedure.GetType(), procedure);
            _procedures.Add(procedure);
        }

        /// <inheritdoc />
        public void RegisterTransitionCondition<TFrom, TTo>(ITransitionCondition<Type> condition)
            where TFrom : IProcedure
            where TTo : IProcedure
        {
            _fsm.RegisterTransitionCondition(typeof(TFrom), typeof(TTo), condition);
        }

        /// <inheritdoc />
        public void Start<T>() where T : IProcedure
        {
            if (_isStarted) return;
            _fsm.ChangeState(typeof(T));
            _isStarted = true;
        }

        /// <inheritdoc />
        public void ChangeProcedure<T>() where T : IProcedure => _fsm.ChangeState(typeof(T));

        /// <inheritdoc />
        public bool TryChangeProcedure<T>() where T : IProcedure => _fsm.TryChangeState(typeof(T));

        #endregion
    }
}
