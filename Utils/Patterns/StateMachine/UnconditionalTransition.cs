using System;

namespace MLGWorks.Utils.Patterns.StateMachine.Interfaces
{
    public class UnconditionalTransition : ITransition
    {
        private StateMachine fsm = null;

        public IState From { get; }
        public IState To { get; }

        public UnconditionalTransition(IState from, IState to)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            To = to ?? throw new ArgumentNullException(nameof(to));
        }

        // Always ready to transition
        public bool ShouldTransition() => true;

        public void SetFSM(StateMachine fsm)
        {
            this.fsm = fsm;
        }
    }
}
