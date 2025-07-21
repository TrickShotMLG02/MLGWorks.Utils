using MLGWorks.Utils.Patterns.StateMachine;
using MLGWorks.Utils.Patterns.StateMachine.Interfaces;
using NUnit.Framework;
using System;

namespace MLGWorks.Utils.Tests.Patterns
{
    public class StateMachineTests
    {
        private StateMachine _fsm;

        [SetUp]
        public void SetUp()
        {
            _fsm = new StateMachine();
        }

        #region Test Helpers

        private class TestState : IState
        {
            public bool Entered { get; private set; }
            public bool Exited { get; private set; }
            public int TickCount { get; private set; }

            public void Reset()
            {
                Entered = false;
                Exited = false;
                TickCount = 0;
            }

            public void OnEnter() => Entered = true;

            public void OnExit() => Exited = true;

            public void Tick() => TickCount++;
        }

        private class ParamState : IStateWithParams<string>
        {
            public bool Entered { get; private set; }
            public bool Exited { get; private set; }
            public string Received { get; private set; }
            public int TickCount { get; private set; }

            public void SetParameters(string parameters) => Received = parameters;

            public void OnEnter() => Entered = true;

            public void OnExit() => Exited = true;

            public void Tick() => TickCount++;
        }

        private class MockCondition : ITransitionCondition
        {
            public bool ShouldReturn { get; set; }
            public int EvaluateCount { get; private set; }

            public bool ShouldTransition()
            {
                EvaluateCount++;
                return ShouldReturn;
            }
        }

        #endregion Test Helpers

        [Test]
        public void ManualChangeState_InvokesEnterExitCorrectly()
        {
            var a = new TestState();
            var b = new TestState();

            _fsm.ChangeState(a);
            Assert.AreSame(a, _fsm.Current);
            Assert.IsTrue(a.Entered);
            Assert.IsFalse(a.Exited);

            _fsm.ChangeState(b);
            Assert.AreSame(b, _fsm.Current);
            Assert.IsTrue(a.Exited);
            Assert.IsTrue(b.Entered);
        }

        [Test]
        public void ChangeState_SameState_DoesNotReenterOrExit()
        {
            var s = new TestState();
            _fsm.ChangeState(s);

            s.Reset();

            _fsm.ChangeState(s);
            Assert.IsFalse(s.Entered);
            Assert.IsFalse(s.Exited);
        }

        [Test]
        public void Tick_WithoutTransition_DelegatesToTick()
        {
            var s = new TestState();
            _fsm.ChangeState(s);

            _fsm.Tick();
            _fsm.Tick();

            Assert.AreEqual(2, s.TickCount);
        }

        [Test]
        public void UnconditionalTransition_ExecutesOnNextTick()
        {
            var from = new TestState();
            var to = new TestState();

            _fsm.AddTransition(new UnconditionalTransition(from, to));
            _fsm.ChangeState(from);

            // After one tick, should transition unconditionally
            _fsm.Tick();
            Assert.AreSame(to, _fsm.Current);
            Assert.IsTrue(from.Exited);
            Assert.IsTrue(to.Entered);
        }

        [Test]
        public void ConditionalTransition_True_TriggersTransition()
        {
            var from = new TestState();
            var to = new TestState();
            var cond = new MockCondition { ShouldReturn = true };

            _fsm.AddTransition(new ConditionalTransition(from, to, cond));
            _fsm.ChangeState(from);

            _fsm.Tick();
            Assert.AreSame(to, _fsm.Current);
            Assert.AreEqual(1, cond.EvaluateCount);
        }

        [Test]
        public void ConditionalTransition_False_DoesNotTrigger()
        {
            var from = new TestState();
            var to = new TestState();
            var cond = new MockCondition { ShouldReturn = false };

            _fsm.AddTransition(new ConditionalTransition(from, to, cond));
            _fsm.ChangeState(from);

            _fsm.Tick();
            Assert.AreSame(from, _fsm.Current);
            Assert.AreEqual(1, cond.EvaluateCount);
        }

        [Test]
        public void MultipleConditionalTransitions_FirstTrueOnly()
        {
            var from = new TestState();
            var t1 = new TestState();
            var t2 = new TestState();
            var cond1 = new MockCondition { ShouldReturn = false };
            var cond2 = new MockCondition { ShouldReturn = true };

            _fsm.AddTransition(new ConditionalTransition(from, t1, cond1));
            _fsm.AddTransition(new ConditionalTransition(from, t2, cond2));
            _fsm.ChangeState(from);

            _fsm.Tick();
            Assert.AreSame(t2, _fsm.Current);
            Assert.AreEqual(1, cond1.EvaluateCount);
            Assert.AreEqual(1, cond2.EvaluateCount);
        }

        [Test]
        public void ParameterizedState_ReceivesParametersAndTicks()
        {
            var paramState = new ParamState();

            _fsm.ChangeState(paramState, "hello");
            Assert.IsTrue(paramState.Entered);
            Assert.AreEqual("hello", paramState.Received);

            _fsm.Tick();
            Assert.AreEqual(1, paramState.TickCount);
        }

        [Test]
        public void ClearTransitions_RemovesAllTransitions()
        {
            var from = new TestState();
            var to = new TestState();
            var cond = new MockCondition { ShouldReturn = true };

            _fsm.AddTransition(new ConditionalTransition(from, to, cond));
            _fsm.ClearTransitions();

            _fsm.ChangeState(from);
            _fsm.Tick();
            Assert.AreSame(from, _fsm.Current);
            Assert.AreEqual(0, cond.EvaluateCount);
        }

        [Test]
        public void ChangeState_Null_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => _fsm.ChangeState((IState)null));
        }

        [Test]
        public void ChangeStateWithParams_Null_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => _fsm.ChangeState<string>(null, "x"));
        }

        [Test]
        public void Tick_WithoutState_DoesNothing()
        {
            Assert.DoesNotThrow(() => _fsm.Tick());
        }
    }
}
