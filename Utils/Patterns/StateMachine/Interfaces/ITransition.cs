namespace MLGWorks.Utils.Patterns.StateMachine.Interfaces
{
    /// <summary>
    /// Represents a transition from one state to another.
    /// </summary>
    public interface ITransition
    {
        /// <summary>
        /// Gets the origin state for this transition.
        /// </summary>
        IState From { get; }

        /// <summary>
        /// Gets the target state for this transition.
        /// </summary>
        IState To { get; }

        /// <summary>
        /// Sets the fsm that is associated with this transition.
        /// </summary>
        /// <param name="fsm"></param>
        public void SetFSM(StateMachine fsm);

        /// <summary>
        /// Determines whether the transition should occur.
        /// </summary>
        /// <returns>True if the transition should be triggered; otherwise, false.</returns>
        bool ShouldTransition();
    }
}
