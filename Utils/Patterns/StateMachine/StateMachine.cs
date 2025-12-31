using MLGWorks.Utils.Patterns.StateMachine.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MLGWorks.Utils.Patterns.StateMachine
{
    /// <summary>
    /// A modular state machine that supports conditional transitions.
    /// </summary>
    [Serializable]
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
            transition.SetFSM(this);
            transition.From.SetFSM(this);
            transition.To.SetFSM(this);
            _transitions.Add(transition);
        }

        /// <summary>
        /// Registers an unconditional transition from one state to another.
        /// Whenever the current state matches, the machine will transit on the next Tick().
        /// </summary>
        public void AddTransition(IState from, IState to)
        {
            from.SetFSM(this);
            to.SetFSM(this);

            UnconditionalTransition trans = new UnconditionalTransition(from, to);
            trans.SetFSM(this);

            AddTransition(trans);
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
        /// Sets the current state to the specified starting state.
        /// </summary>
        /// <param name="startingState">The state to start from</param>
        public void SetStartingState(IState startingState)
        {
            _current = startingState;
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
