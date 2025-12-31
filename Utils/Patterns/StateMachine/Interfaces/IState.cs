namespace MLGWorks.Utils.Patterns.StateMachine.Interfaces
{
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

        /// <summary>
        /// Sets the fsm that is associated with this transition.
        /// </summary>
        /// <param name="fsm"></param>
        public void SetFSM(StateMachine fsm);
    }
}
