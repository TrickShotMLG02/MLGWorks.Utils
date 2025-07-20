using System;

namespace MLGWorks.Utils.Patterns
{
    #region Interfaces

    /// <summary>
    /// Represents a single state in a state machine.
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Called when the state is entered.
        /// </summary>
        void OnEnter();

        /// <summary>
        /// Called when the state is exited.
        /// </summary>
        void OnExit();

        /// <summary>
        /// Called every frame while the state is active.
        /// </summary>
        void Tick();
    }

    #endregion Interfaces

    /// <summary>
    /// A simple state machine for switching between different states.
    /// </summary>
    public class StateMachine
    {
        private IState _current;

        /// <summary>
        /// Gets the currently active state.
        /// </summary>
        public IState Current => _current;

        /// <summary>
        /// Switches the current state to the given new state.
        /// </summary>
        /// <param name="newState">The new state to switch to.</param>
        public void ChangeState(IState newState)
        {
            if (_current == newState)
                return;

            _current?.OnExit();
            _current = newState;
            _current?.OnEnter();
        }

        /// <summary>
        /// Ticks the currently active state.
        /// This should typically be called once per frame.
        /// </summary>
        public void Tick()
        {
            _current?.Tick();
        }
    }
}
