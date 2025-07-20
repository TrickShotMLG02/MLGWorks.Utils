using NUnit.Framework;
using System;

namespace MLGWorks.Utils.Tests.Patterns
{
    public class StateMachineTests
    {
        public class StateMachine<TState>
        {
            public TState CurrentState { get; private set; }

            public event Action<TState, TState> OnStateChanged;

            public StateMachine(TState initialState)
            {
                CurrentState = initialState;
            }

            public void SetState(TState newState)
            {
                if (!Equals(CurrentState, newState))
                {
                    TState oldState = CurrentState;
                    CurrentState = newState;
                    OnStateChanged?.Invoke(oldState, newState);
                }
            }
        }

        private enum GameState
        { Menu, Playing, Paused }

        [Test]
        public void StateMachine_Starts_With_Initial_State()
        {
            var sm = new StateMachine<GameState>(GameState.Menu);
            Assert.AreEqual(GameState.Menu, sm.CurrentState);
        }

        [Test]
        public void StateMachine_Changes_State()
        {
            var sm = new StateMachine<GameState>(GameState.Menu);
            sm.SetState(GameState.Playing);
            Assert.AreEqual(GameState.Playing, sm.CurrentState);
        }

        [Test]
        public void StateMachine_Does_Not_Trigger_Change_For_Same_State()
        {
            var sm = new StateMachine<GameState>(GameState.Menu);
            bool triggered = false;
            sm.OnStateChanged += (_, _) => triggered = true;

            sm.SetState(GameState.Menu);
            Assert.IsFalse(triggered);
        }

        [Test]
        public void StateMachine_Triggers_OnStateChanged_Event()
        {
            var sm = new StateMachine<GameState>(GameState.Menu);
            GameState from = GameState.Menu, to = GameState.Menu;

            sm.OnStateChanged += (oldState, newState) =>
            {
                from = oldState;
                to = newState;
            };

            sm.SetState(GameState.Playing);

            Assert.AreEqual(GameState.Menu, from);
            Assert.AreEqual(GameState.Playing, to);
        }
    }
}
