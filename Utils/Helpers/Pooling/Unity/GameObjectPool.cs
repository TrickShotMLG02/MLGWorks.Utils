using System;
using System.Collections.Generic;
using UnityEngine;
using MLGWorks.Utils.Helpers.Pooling.Core;
using MLGWorks.Utils.Helpers.Unity;

namespace MLGWorks.Utils.Helpers.Pooling.Unity
{
    /// <summary>
    /// Pools instances of a prefab and manages their active state and parent transform.
    /// Instances must be released to the pool that created them.
    /// </summary>
    /// <remarks>All operations must be performed on Unity's main thread.</remarks>
    public sealed class GameObjectPool : IDisposable
    {
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly ObjectPool<GameObject> pool;

        /// <summary>Gets the number of inactive instances available for reuse.</summary>
        public int CountInactive => pool.CountInactive;

        /// <summary>Gets the number of instances currently checked out.</summary>
        public int CountActive => pool.CountActive;

        /// <summary>Gets the total number of instances owned by this pool.</summary>
        public int CountAll => pool.CountAll;

        /// <summary>
        /// Creates a GameObject pool for a prefab.
        /// </summary>
        /// <param name="prefab">The prefab to instantiate.</param>
        /// <param name="parent">Optional parent for inactive instances.</param>
        /// <param name="initialCapacity">Number of instances to create immediately.</param>
        /// <param name="maxCapacity">Maximum retained instances; use -1 for unlimited.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="prefab"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for invalid capacity values.</exception>
        /// <exception cref="ArgumentException">Thrown when initial capacity exceeds maximum capacity.</exception>
        public GameObjectPool(GameObject prefab, Transform parent = null, int initialCapacity = 0, int maxCapacity = -1)
        {
            this.prefab = prefab != null ? prefab : throw new ArgumentNullException(nameof(prefab));
            this.parent = parent;
            pool = new ObjectPool<GameObject>(
                CreateInstance,
                OnGet,
                OnRelease,
                DestroyInstance,
                initialCapacity,
                maxCapacity);
        }

        /// <summary>Gets an inactive prefab instance and activates it.</summary>
        /// <returns>An active prefab instance.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public GameObject Get() => pool.Get();

        /// <summary>Acquires multiple instances and appends them to a caller-provided collection.</summary>
        /// <param name="destination">The collection receiving the instances.</param>
        /// <param name="count">The number of instances to acquire.</param>
        /// <exception cref="ArgumentNullException">Thrown when destination is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public void GetMany(ICollection<GameObject> destination, int count) => pool.GetMany(destination, count);

        /// <summary>
        /// Instantiates and retains additional inactive prefab instances.
        /// Call this during loading or setup to avoid runtime instantiation spikes.
        /// </summary>
        /// <param name="count">The number of additional instances to create.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative or exceeds the remaining capacity.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public void Prewarm(int count) => pool.Prewarm(count);

        /// <summary>Creates up to <paramref name="count"/> inactive instances during one prewarming step.</summary>
        /// <param name="count">The maximum number of instances to create.</param>
        /// <returns>The number of instances created.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public int PrewarmStep(int count) => pool.PrewarmStep(count);

        /// <summary>Releases and deactivates a prefab instance.</summary>
        /// <param name="instance">The instance to release.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="instance"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the instance belongs to another pool or is already released.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public void Release(GameObject instance) => pool.Release(instance);

        /// <summary>Releases every instance in an enumerable sequence.</summary>
        /// <param name="instances">The active instances to release.</param>
        /// <exception cref="ArgumentNullException">Thrown when instances is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when an instance is not active in this pool.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public void ReleaseMany(IEnumerable<GameObject> instances) => pool.ReleaseMany(instances);

        /// <summary>Destroys all inactive instances while retaining active instances.</summary>
        public void Clear() => pool.Clear();

        /// <summary>Destroys all instances owned by the pool and prevents further use.</summary>
        public void Dispose() => pool.Dispose();

        private GameObject CreateInstance()
        {
            GameObject instance = UnityEngine.Object.Instantiate(prefab, parent, false);
            instance.SetActiveIfChanged(false);
            return instance;
        }

        private void OnGet(GameObject instance)
        {
            if (parent != null)
            {
                instance.transform.SetParentIfChanged(parent);
            }

            instance.SetActiveIfChanged(true);
        }

        private void OnRelease(GameObject instance)
        {
            instance.SetActiveIfChanged(false);
            if (parent != null)
            {
                instance.transform.SetParentIfChanged(parent);
            }
        }

        private static void DestroyInstance(GameObject instance)
        {
            instance.SafeDestroy();
        }
    }
}
