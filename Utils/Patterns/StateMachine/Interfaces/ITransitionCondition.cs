namespace MLGWorks.Utils.Patterns.StateMachine.Interfaces
{
    /// <summary>
    /// Represents a condition that determines whether a state transition should occur.
    /// </summary>
    public interface ITransitionCondition
    {
        /// <summary>
        /// Sets the fsm that is associated with this transition.
        /// </summary>
        /// <param name="fsm"></param>
        public void SetFSM(StateMachine fsm);

        /// <summary>
        /// Evaluates whether the condition is met.
        /// </summary>
        /// <returns>True if the transition should occur, false otherwise.</returns>
        bool ShouldTransition();
    }
}
