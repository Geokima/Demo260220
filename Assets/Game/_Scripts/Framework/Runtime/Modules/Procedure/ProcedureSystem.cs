using System;
using System.Collections.Generic;
using Framework.Modules.FSM;
using UnityEngine;

namespace Framework.Modules.Procedure
{
    public class ProcedureSystem : AbstractSystem
    {
        private FSM<Type> _fsm;
        private bool _isStarted;
        private readonly List<IProcedure> _procedures = new List<IProcedure>();

        public Type CurrentProcedure => _fsm?.CurrentState;
        public bool IsInProcedure<T>() where T : IProcedure => _fsm?.IsInState(typeof(T)) ?? false;

        public event Action<Type, Type> OnProcedureChanged
        {
            add { if (_fsm != null) _fsm.OnStateChanged += value; }
            remove { if (_fsm != null) _fsm.OnStateChanged -= value; }
        }

        public override void Init()
        {
            _fsm = new FSM<Type>();
            _fsm.OnStateChanged += (from, to) =>
            {
                Debug.Log($"[Procedure] Transition: {from?.Name} -> {to?.Name}");
            };
        }

        public void RegisterProcedure(IProcedure procedure)
        {
            procedure.Architecture = Architecture;
            if (procedure is ProcedureBase procedureBase)
                procedureBase.Owner = this;

            procedure.OnInit();
            _fsm.RegisterState(procedure.GetType(), procedure);
            _procedures.Add(procedure);
        }

        public void RegisterTransitionCondition<TFrom, TTo>(ITransitionCondition<Type> condition)
            where TFrom : IProcedure
            where TTo : IProcedure
        {
            _fsm.RegisterTransitionCondition(typeof(TFrom), typeof(TTo), condition);
        }

        public void Start<T>() where T : IProcedure
        {
            if (_isStarted) return;
            _fsm.ChangeState(typeof(T));
            _isStarted = true;
        }

        public void ChangeProcedure<T>() where T : IProcedure => _fsm.ChangeState(typeof(T));
        public bool TryChangeProcedure<T>() where T : IProcedure => _fsm.TryChangeState(typeof(T));

        public void Update() => _fsm.Update();
        public void FixedUpdate() => _fsm.FixedUpdate();

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
    }
}
