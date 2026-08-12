using System;

namespace MLGWorks.Utils.Patterns.Singletons
{
    /// <summary>Provides a lazily-created singleton base for regular C# classes.</summary>
    /// <typeparam name="T">The concrete class type inheriting from <see cref="PureSingleton{T}"/>.</typeparam>
    /// <remarks>
    /// The instance is created on first access and is shared for the lifetime of the
    /// application domain. Prefer dependency injection when explicit ownership and
    /// easy replacement in tests are more important than global access.
    /// </remarks>
    public abstract class PureSingleton<T> where T : PureSingleton<T>, new()
    {
        /// <summary>Gets the lazily-created singleton instance.</summary>
        /// <remarks>
        /// The concrete type must expose a public parameterless constructor to satisfy
        /// the generic <c>new()</c> constraint. Accessing this property for the first
        /// time creates the instance; subsequent accesses return the same object.
        /// </remarks>
        public static T Instance => SingletonState<T>.GetOrCreate(() => new T());
    }
}
