using NUnit.Framework;
using Framework.Modules.FSM;

namespace Tests.EditMode.FSM
{
    public class FSMTests
    {
        private enum TestState { State1, State2 }

        private class DummyState : IState
        {
            public int EnterCount = 0;
            public int ExitCount = 0;
            public void OnEnter() => EnterCount++;
            public void OnExit() => ExitCount++;
            public void OnFixedUpdate() {}
            public void OnUpdate() {}
        }

        [Test]
        public void FSM_StateTransition_ShouldWork()
        {
            var fsm = new Framework.Modules.FSM.FSM<TestState>();
            var state1 = new DummyState();
            var state2 = new DummyState();

            fsm.RegisterState(TestState.State1, state1);
            fsm.RegisterState(TestState.State2, state2);

            fsm.ChangeState(TestState.State1);
            Assert.IsTrue(fsm.IsInState(TestState.State1));
            Assert.AreEqual(1, state1.EnterCount);

            fsm.ChangeState(TestState.State2);
            Assert.IsTrue(fsm.IsInState(TestState.State2));
            Assert.AreEqual(1, state1.ExitCount);
            Assert.AreEqual(1, state2.EnterCount);
        }
    }
}
