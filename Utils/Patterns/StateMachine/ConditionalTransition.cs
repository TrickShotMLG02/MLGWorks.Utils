namespace MLGWorks.Utils.Patterns.StateMachine.Interfaces
{
    /// <summary>
    /// A transition that uses a condition to determine if it should occur.
    /// </summary>
    public class ConditionalTransition : ITransition
    {
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
            From = from;
            To = to;
            _condition = condition;
        }

        /// <inheritdoc/>
        public bool ShouldTransition() => _condition.ShouldTransition();
    }
}
