using System;
using System.Threading;

namespace MLGWorks.Utils.Patterns
{
    /// <summary>Represents one active event-bus subscription.</summary>
    public sealed class EventSubscription : IDisposable
    {
        private Action disposeAction;

        internal EventSubscription(Action disposeAction)
        {
            this.disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
        }

        /// <summary>Removes the subscription. Repeated calls are safe.</summary>
        public void Dispose()
        {
            Interlocked.Exchange(ref disposeAction, null)?.Invoke();
            GC.SuppressFinalize(this);
        }
    }
}
