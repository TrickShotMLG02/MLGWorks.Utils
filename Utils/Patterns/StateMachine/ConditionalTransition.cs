using MLGWorks.Utils.Patterns.StateMachine.Interfaces;
using System;

namespace MLGWorks.Utils.Patterns.StateMachine
{
    /// <summary>
    /// A transition that uses a condition to determine if it should occur.
    /// </summary>
    public class ConditionalTransition : ITransition
    {
        private StateMachine fsm = null;

        /// <inheritdoc/>
        public IState From { get; }

        /// <inheritdoc/>
        public IState To { get; }

        private readonly ITransitionCondition _condition;

        /// <summary>
        /// Creates a new conditional transition.
        /// </summary>
        /// <param name="from">Origin state.</param>
        /// <param name="to">Target state.</param>
        /// <param name="condition">Condition to evaluate.</param>
        public ConditionalTransition(IState from, IState to, ITransitionCondition condition)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
            _condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        /// <inheritdoc/>
        public bool ShouldTransition() => _condition.ShouldTransition();

        public void SetFSM(StateMachine fsm)
        {
            this.fsm = fsm;
            _condition.SetFSM(fsm);
        }
    }
}
