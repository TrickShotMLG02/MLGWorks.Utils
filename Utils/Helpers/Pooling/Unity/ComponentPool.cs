using System;
using System.Collections.Generic;
using UnityEngine;
using MLGWorks.Utils.Helpers.Pooling.Core;

namespace MLGWorks.Utils.Helpers.Pooling.Unity
{
    /// <summary>
    /// Pools a component attached to the root of a prefab while reusing the shared
    /// <see cref="GameObjectPool"/> lifecycle and capacity behavior.
    /// </summary>
    /// <typeparam name="T">The component type attached to the prefab root.</typeparam>
    /// <remarks>
    /// Components implementing <see cref="IPoolable"/> receive acquire and release
    /// callbacks. This makes the type suitable for projectiles, UI controllers, and
    /// other components with application-specific state to reset.
    /// </remarks>
    public sealed class ComponentPool<T> : IDisposable where T : Component
    {
        private readonly GameObjectPool gameObjectPool;

        /// <summary>Gets the number of inactive components available for reuse.</summary>
        public int CountInactive => gameObjectPool.CountInactive;

        /// <summary>Gets the number of components currently checked out.</summary>
        public int CountActive => gameObjectPool.CountActive;

        /// <summary>Gets the total number of components owned by this pool.</summary>
        public int CountAll => gameObjectPool.CountAll;

        /// <summary>
        /// Creates a component pool from a prefab whose root contains <typeparamref name="T"/>.
        /// </summary>
        /// <param name="prefab">The prefab containing the component on its root.</param>
        /// <param name="parent">Optional parent for pooled instances.</param>
        /// <param name="initialCapacity">Number of instances to create immediately.</param>
        /// <param name="maxCapacity">Maximum retained instances; use -1 for unlimited.</param>
        /// <exception cref="ArgumentNullException">Thrown when prefab is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the prefab root lacks the requested component.</exception>
        public ComponentPool(GameObject prefab, Transform parent = null, int initialCapacity = 0, int maxCapacity = -1)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (prefab.GetComponent<T>() == null)
            {
                throw new ArgumentException(
                    $"The prefab root must contain a {typeof(T).Name} component.", nameof(prefab));
            }

            gameObjectPool = new GameObjectPool(prefab, parent, initialCapacity, maxCapacity);
        }

        /// <summary>Acquires a component and invokes its pool acquire hook when available.</summary>
        /// <returns>An active pooled component.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public T Get()
        {
            GameObject instance = gameObjectPool.Get();
            T component = instance.GetComponent<T>();
            try
            {
                if (component is IPoolable poolable) poolable.OnPoolAcquire();
                return component;
            }
            catch
            {
                gameObjectPool.Release(instance);
                throw;
            }
        }

        /// <summary>Acquires multiple components and appends them to a collection.</summary>
        /// <param name="destination">The collection receiving the components.</param>
        /// <param name="count">The number of components to acquire.</param>
        /// <exception cref="ArgumentNullException">Thrown when destination is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public void GetMany(ICollection<T> destination, int count)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            for (int i = 0; i < count; i++) destination.Add(Get());
        }

        /// <summary>Creates additional inactive component instances.</summary>
        /// <param name="count">The number of instances to create.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative or exceeds capacity.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public void Prewarm(int count) => gameObjectPool.Prewarm(count);

        /// <summary>Creates up to the requested number of inactive instances in one step.</summary>
        /// <param name="count">The maximum number of instances to create.</param>
        /// <returns>The number of instances created.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public int PrewarmStep(int count) => gameObjectPool.PrewarmStep(count);

        /// <summary>Releases a component and invokes its pool release hook when available.</summary>
        /// <param name="component">The active component to release.</param>
        /// <exception cref="ArgumentNullException">Thrown when component is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the component is not active in this pool.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public void Release(T component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            try
            {
                if (component is IPoolable poolable) poolable.OnPoolRelease();
            }
            finally
            {
                gameObjectPool.Release(component.gameObject);
            }
        }

        /// <summary>Releases every component in an enumerable sequence.</summary>
        /// <param name="components">The active components to release.</param>
        /// <exception cref="ArgumentNullException">Thrown when components is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when a component is not active in this pool.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public void ReleaseMany(IEnumerable<T> components)
        {
            if (components == null) throw new ArgumentNullException(nameof(components));
            foreach (T component in components) Release(component);
        }

        /// <summary>Destroys all inactive instances while retaining active instances.</summary>
        public void Clear() => gameObjectPool.Clear();

        /// <summary>Destroys all instances and prevents further use.</summary>
        public void Dispose() => gameObjectPool.Dispose();
    }
}
