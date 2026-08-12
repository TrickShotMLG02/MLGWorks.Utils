using System;

namespace MLGWorks.Utils.Patterns.Singletons
{
    /// <summary>Stores the shared instance state used by singleton adapters.</summary>
    /// <typeparam name="T">The type of object stored by this state.</typeparam>
    internal static class SingletonState<T> where T : class
    {
        private static readonly object SyncRoot = new object();
        private static T instance;

        /// <summary>Gets the currently stored instance, or <see langword="null"/>.</summary>
        public static T Current => instance;

        /// <summary>Attempts to store an instance when no instance is currently stored.</summary>
        /// <param name="value">The instance to store.</param>
        /// <returns>
        /// <see langword="true"/> when the value became or already was the current instance;
        /// otherwise, <see langword="false"/> when another instance owns the state.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static bool TrySet(T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            lock (SyncRoot)
            {
                if (instance != null)
                {
                    return ReferenceEquals(instance, value);
                }

                instance = value;
                return true;
            }
        }

        /// <summary>Gets the current instance or creates it using the supplied factory.</summary>
        /// <param name="factory">The factory used when no instance exists.</param>
        /// <returns>The existing or newly-created instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the factory returns null.</exception>
        public static T GetOrCreate(Func<T> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            lock (SyncRoot)
            {
                return instance ??= factory() ?? throw new InvalidOperationException(
                    $"The singleton factory for {typeof(T)} returned null.");
            }
        }

        /// <summary>Clears the stored instance when it matches the supplied value.</summary>
        /// <param name="value">The instance being removed.</param>
        public static void Clear(T value)
        {
            lock (SyncRoot)
            {
                if (ReferenceEquals(instance, value))
                {
                    instance = null;
                }
            }
        }
    }
}
