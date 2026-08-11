using System;
using System.Collections.Generic;

namespace MLGWorks.Utils.Patterns
{
    /// <summary>Owns multiple disposable resources and disposes them as one scope.</summary>
    /// <remarks>This type is not thread-safe and should be accessed from one execution context.</remarks>
    public sealed class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> disposables = new();
        private bool disposed;

        /// <summary>Gets the number of resources currently owned by this scope.</summary>
        public int Count => disposables.Count;

        /// <summary>Adds a disposable resource to the scope.</summary>
        /// <param name="disposable">The resource to own.</param>
        /// <exception cref="ArgumentNullException">Thrown when disposable is null.</exception>
        public void Add(IDisposable disposable)
        {
            if (disposable == null) throw new ArgumentNullException(nameof(disposable));

            if (disposed)
            {
                disposable.Dispose();
                return;
            }

            disposables.Add(disposable);
        }

        /// <summary>Disposes all owned resources and prevents further retention.</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            for (int i = disposables.Count - 1; i >= 0; i--)
            {
                disposables[i].Dispose();
            }

            disposables.Clear();
            GC.SuppressFinalize(this);
        }
    }
}
