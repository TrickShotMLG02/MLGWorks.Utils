using System;
using System.Collections.Generic;

namespace MLGWorks.Utils.Helpers.Pooling.Core
{
    /// <summary>
    /// A reusable pool for reference-type objects. The pool has no global state and
    /// does not impose a reset convention; callers can provide lifecycle callbacks.
    /// </summary>
    /// <remarks>This type is not thread-safe and should be accessed from one execution context.</remarks>
    /// <typeparam name="T">The reference type managed by the pool.</typeparam>
    public sealed class ObjectPool<T> : IDisposable where T : class
    {
        private readonly Func<T> factory;
        private readonly Action<T> onGet;
        private readonly Action<T> onRelease;
        private readonly Action<T> onDestroy;
        private readonly Stack<T> inactive;
        private readonly HashSet<T> active;
        private readonly int maxCapacity;
        private bool disposed;

        /// <summary>Gets the number of objects currently available for reuse.</summary>
        public int CountInactive => inactive.Count;

        /// <summary>Gets the number of objects currently checked out of the pool.</summary>
        public int CountActive => active.Count;

        /// <summary>Gets the total number of objects owned by the pool.</summary>
        public int CountAll => CountInactive + CountActive;

        /// <summary>
        /// Creates a pool.
        /// </summary>
        /// <param name="factory">Creates a new object when the pool is empty.</param>
        /// <param name="onGet">Optional callback invoked after an object is acquired.</param>
        /// <param name="onRelease">Optional callback invoked before an object is retained.</param>
        /// <param name="onDestroy">Optional callback invoked when an object is discarded.</param>
        /// <param name="initialCapacity">Number of objects to create immediately.</param>
        /// <param name="maxCapacity">Maximum number of objects retained by the pool; use -1 for unlimited.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for invalid capacity values.</exception>
        public ObjectPool(
            Func<T> factory,
            Action<T> onGet = null,
            Action<T> onRelease = null,
            Action<T> onDestroy = null,
            int initialCapacity = 0,
            int maxCapacity = -1)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
            this.onGet = onGet;
            this.onRelease = onRelease;
            this.onDestroy = onDestroy;
            this.maxCapacity = maxCapacity;

            if (initialCapacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            }

            if (maxCapacity == 0 || maxCapacity < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCapacity));
            }

            if (maxCapacity >= 0 && initialCapacity > maxCapacity)
            {
                throw new ArgumentException(
                    "Initial capacity cannot exceed maximum capacity.", nameof(initialCapacity));
            }

            inactive = new Stack<T>(initialCapacity);
            active = new HashSet<T>(initialCapacity);

            for (int i = 0; i < initialCapacity; i++)
            {
                inactive.Push(Create());
            }
        }

        /// <summary>
        /// Acquires an object from the pool, creating one when no inactive object exists.
        /// </summary>
        /// <returns>An active pooled object.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the factory returns null.</exception>
        public T Get()
        {
            ThrowIfDisposed();

            T item = inactive.Count > 0 ? inactive.Pop() : Create();
            active.Add(item);

            try
            {
                if (item is IPoolable poolable)
                {
                    poolable.OnPoolAcquire();
                }

                onGet?.Invoke(item);
                return item;
            }
            catch
            {
                active.Remove(item);
                Destroy(item);
                throw;
            }
        }

        /// <summary>
        /// Acquires multiple objects and appends them to a caller-provided collection.
        /// </summary>
        /// <param name="destination">The collection receiving the objects.</param>
        /// <param name="count">The number of objects to acquire.</param>
        /// <exception cref="ArgumentNullException">Thrown when destination is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public void GetMany(ICollection<T> destination, int count)
        {
            ThrowIfDisposed();
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            for (int i = 0; i < count; i++)
            {
                destination.Add(Get());
            }
        }

        /// <summary>
        /// Creates and retains additional inactive objects without invoking get or release callbacks.
        /// Calling this before gameplay avoids allocation and instantiation spikes during use.
        /// </summary>
        /// <param name="count">The number of additional objects to create.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative or exceeds the remaining capacity.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public void Prewarm(int count)
        {
            ThrowIfDisposed();
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (maxCapacity >= 0 && count > maxCapacity - CountAll)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Prewarm count exceeds the remaining pool capacity.");
            }

            for (int i = 0; i < count; i++)
            {
                inactive.Push(Create());
            }
        }

        /// <summary>
        /// Creates up to the requested number of inactive objects during one caller-controlled step.
        /// Call this once per frame to distribute a large prewarm operation.
        /// </summary>
        /// <param name="count">The maximum number of objects to create.</param>
        /// <returns>The number of objects created.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when count is negative.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public int PrewarmStep(int count)
        {
            ThrowIfDisposed();
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

            int available = maxCapacity < 0 ? count : Math.Min(count, maxCapacity - CountAll);
            for (int i = 0; i < available; i++)
            {
                inactive.Push(Create());
            }

            return available;
        }

        /// <summary>
        /// Returns an active object to the pool or destroys it when the retention limit is reached.
        /// </summary>
        /// <param name="item">The object to release.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the object is not active in this pool.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public void Release(T item)
        {
            ThrowIfDisposed();
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!active.Remove(item))
            {
                throw new InvalidOperationException("The object is not active in this pool.");
            }

            try
            {
                if (item is IPoolable poolable)
                {
                    poolable.OnPoolRelease();
                }

                onRelease?.Invoke(item);
            }
            catch
            {
                Destroy(item);
                throw;
            }

            if (maxCapacity >= 0 && CountAll >= maxCapacity)
            {
                Destroy(item);
            }
            else
            {
                inactive.Push(item);
            }
        }

        /// <summary>Releases every object in an enumerable sequence.</summary>
        /// <param name="items">The active objects to release.</param>
        /// <exception cref="ArgumentNullException">Thrown when items is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when an item is not active in this pool.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public void ReleaseMany(IEnumerable<T> items)
        {
            ThrowIfDisposed();
            if (items == null) throw new ArgumentNullException(nameof(items));

            foreach (T item in items)
            {
                Release(item);
            }
        }

        /// <summary>
        /// Destroys all inactive objects while leaving active objects checked out.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
        public void Clear()
        {
            ThrowIfDisposed();
            while (inactive.Count > 0)
            {
                Destroy(inactive.Pop());
            }
        }

        /// <summary>
        /// Destroys all objects owned by the pool and prevents further use.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            while (inactive.Count > 0)
            {
                Destroy(inactive.Pop());
            }

            foreach (T item in active)
            {
                Destroy(item);
            }

            active.Clear();
            GC.SuppressFinalize(this);
        }

        private T Create()
        {
            T item = factory();
            if (item == null)
            {
                throw new InvalidOperationException("The pool factory returned null.");
            }

            return item;
        }

        private void Destroy(T item)
        {
            try
            {
                onDestroy?.Invoke(item);
            }
            catch
            {
                // Cleanup must continue for the remaining pooled objects.
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ObjectPool<T>));
            }
        }
    }
}
