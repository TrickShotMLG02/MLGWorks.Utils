namespace MLGWorks.Utils.Patterns.StateMachine.Interfaces
{
    /// <summary>
    /// Represents a state that can receive parameters of type <typeparamref name="TParams"/>.
    /// </summary>
    /// <typeparam name="TParams">The type of parameters this state accepts.</typeparam>
    public interface IStateWithParams<TParams> : IState
    {
        /// <summary>
        /// Provides parameters to the state before or during <see cref="OnEnter"/>.
        /// </summary>
        /// <param name="parameters">The parameters to initialize the state.</param>
        void SetParameters(TParams parameters);

        TParams GetParameters();
    }
}
