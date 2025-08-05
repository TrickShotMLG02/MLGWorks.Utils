using MLGWorks.Utils.Patterns.StateMachine.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MLGWorks.Utils.Patterns.StateMachine
{
    /// <summary>
    /// A modular state machine that supports conditional transitions.
    /// </summary>
    public class StateMachine
    {
        private IState _current;
        private readonly List<ITransition> _transitions = new();

        /// <summary>
        /// Gets the currently active state.
        /// </summary>
        public IState Current => _current;

        /// <summary>
        /// Registers a transition between states.
        /// </summary>
        /// <param name="transition">The transition to register.</param>
        public void AddTransition(ITransition transition)
        {
            _transitions.Add(transition);
        }

        /// <summary>
        /// Registers an unconditional transition from one state to another.
        /// Whenever the current state matches, the machine will transit on the next Tick().
        /// </summary>
        public void AddTransition(IState from, IState to)
        {
            AddTransition(new UnconditionalTransition(from, to));
        }

        /// <summary>
        /// Clears all transitions from the state machine.
        /// </summary>
        public void ClearTransitions()
        {
            _transitions.Clear();
        }

        /// <summary>
        /// Changes to a new state immediately, bypassing transitions.
        /// Calls <see cref="IState.OnExit"/> on the old state, then <see cref="IState.OnEnter"/> on the new state.
        /// </summary>
        /// <param name="newState">The new state to enter.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="newState"/> is null.</exception>
        public void ChangeState(IState newState)
        {
            if (newState == null)
            {
                throw new ArgumentNullException(nameof(newState));
            }

            if (_current == newState)
            {
                return;
            }

            _current?.OnExit();
            _current = newState;
            _current.OnEnter();
        }

        /// <summary>
        /// Changes the state to a new parameterized state.
        /// Calls <see cref="IState.OnExit"/> on the old state, sets parameters on the new state,
        /// then calls <see cref="IState.OnEnter"/> on the new state.
        /// </summary>
        /// <typeparam name="TParams">The type of parameters the new state accepts.</typeparam>
        /// <param name="newState">The new parameterized state to switch to.</param>
        /// <param name="parameters">The parameters to pass to the new state.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="newState"/> is <c>null</c>.</exception>
        public void ChangeState<TParams>(IStateWithParams<TParams> newState, TParams parameters)
        {
            if (newState == null)
            {
                throw new ArgumentNullException(nameof(newState));
            }

            _current?.OnExit();
            newState.SetParameters(parameters);
            _current = newState;
            _current.OnEnter();
        }

        /// <summary>
        /// Ticks the current state and evaluates transitions.
        /// </summary>
        public void Tick()
        {
            // Check all transitions from the current state
            var transition = _transitions.FirstOrDefault(t => t.From == _current && t.ShouldTransition());

            if (transition != null)
            {
                ChangeState(transition.To);
                return;
            }

            _current?.Tick();
        }
    }
}
