using System;
using System.Collections.Generic;
using Framework.Modules.FSM;
using UnityEngine;

namespace Framework.Modules.Procedure
{
    public class ProcedureSystem : AbstractSystem
    {
        private FSM<ProcedureType> _fsm;
        private bool _isStarted;
        private readonly List<IProcedure> _procedures = new List<IProcedure>();

        public ProcedureType CurrentProcedure => _fsm?.CurrentState ?? ProcedureType.None;
        public bool IsInProcedure(ProcedureType procedure) => _fsm?.IsInState(procedure) ?? false;

        public event Action<ProcedureType, ProcedureType> OnProcedureChanged
        {
            add { if (_fsm != null) _fsm.OnStateChanged += value; }
            remove { if (_fsm != null) _fsm.OnStateChanged -= value; }
        }

        public override void Init()
        {
            _fsm = new FSM<ProcedureType>();
            _fsm.OnStateChanged += (from, to) =>
            {
                Debug.Log($"[Procedure] Transition: {from} -> {to}");
            };
        }

        public void RegisterProcedure(IProcedure procedure)
        {
            procedure.Architecture = Architecture;
            procedure.OnInit();
            _fsm.RegisterState(procedure.Type, procedure);
            _procedures.Add(procedure);
        }

        public void RegisterTransitionCondition(ProcedureType from, ProcedureType to, ITransitionCondition<ProcedureType> condition)
        {
            _fsm.RegisterTransitionCondition(from, to, condition);
        }

        public void Start(ProcedureType initialProcedure)
        {
            if (_isStarted) return;
            _fsm.ChangeState(initialProcedure);
            _isStarted = true;
        }

        public void ChangeProcedure(ProcedureType procedure) => _fsm.ChangeState(procedure);
        public bool TryChangeProcedure(ProcedureType procedure) => _fsm.TryChangeState(procedure);

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
