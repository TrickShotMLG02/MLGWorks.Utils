using System;
using UnityEngine;

namespace MLGWorks.Utils.Helpers.Pooling.Unity
{
    /// <summary>Pools particle-system prefabs and clears their particle state between uses.</summary>
    /// <remarks>The prefab root must contain a <see cref="ParticleSystem"/>.</remarks>
    public sealed class ParticleSystemPool : IDisposable
    {
        private readonly ComponentPool<ParticleSystem> pool;

        /// <summary>Gets the number of inactive particle systems available.</summary>
        public int CountInactive => pool.CountInactive;

        /// <summary>Gets the number of active particle systems.</summary>
        public int CountActive => pool.CountActive;

        /// <summary>
        /// Creates a particle-system pool.
        /// </summary>
        /// <param name="prefab">A prefab whose root contains a particle system.</param>
        /// <param name="parent">Optional parent for pooled instances.</param>
        /// <param name="initialCapacity">Number of instances to create immediately.</param>
        /// <param name="maxCapacity">Maximum retained instances; use -1 for unlimited.</param>
        /// <exception cref="ArgumentNullException">Thrown when prefab is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the prefab root lacks a particle system.</exception>
        public ParticleSystemPool(GameObject prefab, Transform parent = null, int initialCapacity = 0, int maxCapacity = -1)
        {
            pool = new ComponentPool<ParticleSystem>(prefab, parent, initialCapacity, maxCapacity);
        }

        /// <summary>Acquires and starts a particle system after clearing old particles.</summary>
        /// <returns>An active, playing particle system.</returns>
        public ParticleSystem Get()
        {
            ParticleSystem particles = pool.Get();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play(true);
            return particles;
        }

        /// <summary>Stops and clears a particle system before returning it to the pool.</summary>
        /// <param name="particles">The active particle system to release.</param>
        public void Release(ParticleSystem particles)
        {
            if (particles == null) throw new ArgumentNullException(nameof(particles));
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            pool.Release(particles);
        }

        /// <summary>Creates additional inactive particle systems.</summary>
        /// <param name="count">The number of instances to create.</param>
        public void Prewarm(int count) => pool.Prewarm(count);

        /// <summary>Creates up to the requested number of instances in one step.</summary>
        /// <param name="count">The maximum number of instances to create.</param>
        /// <returns>The number of instances created.</returns>
        public int PrewarmStep(int count) => pool.PrewarmStep(count);

        /// <summary>Destroys inactive particle systems.</summary>
        public void Clear() => pool.Clear();

        /// <summary>Destroys all particle systems and prevents further use.</summary>
        public void Dispose() => pool.Dispose();
    }
}
