using System;
using UnityEngine;

namespace MLGWorks.Utils.Helpers.Pooling.Unity
{
    /// <summary>Pools audio-source prefabs and resets playback state between uses.</summary>
    /// <remarks>The prefab root must contain an <see cref="AudioSource"/>.</remarks>
    public sealed class AudioSourcePool : IDisposable
    {
        private readonly ComponentPool<AudioSource> pool;

        /// <summary>Gets the number of inactive audio sources available.</summary>
        public int CountInactive => pool.CountInactive;

        /// <summary>Gets the number of active audio sources.</summary>
        public int CountActive => pool.CountActive;

        /// <summary>Creates an audio-source pool.</summary>
        /// <param name="prefab">A prefab whose root contains an audio source.</param>
        /// <param name="parent">Optional parent for pooled instances.</param>
        /// <param name="initialCapacity">Number of instances to create immediately.</param>
        /// <param name="maxCapacity">Maximum retained instances; use -1 for unlimited.</param>
        /// <exception cref="ArgumentNullException">Thrown when prefab is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the prefab root lacks an audio source.</exception>
        public AudioSourcePool(GameObject prefab, Transform parent = null, int initialCapacity = 0, int maxCapacity = -1)
        {
            pool = new ComponentPool<AudioSource>(prefab, parent, initialCapacity, maxCapacity);
        }

        /// <summary>Acquires an idle audio source and optionally starts a clip.</summary>
        /// <param name="clip">The clip to assign, or null to acquire the source without a clip.</param>
        /// <param name="playImmediately">Whether to start playback immediately.</param>
        /// <returns>An active audio source.</returns>
        public AudioSource Get(AudioClip clip = null, bool playImmediately = true)
        {
            AudioSource source = pool.Get();
            source.Stop();
            if (clip != null) source.clip = clip;
            if (playImmediately) source.Play();
            return source;
        }

        /// <summary>Stops an audio source and clears its clip before returning it.</summary>
        /// <param name="source">The active audio source to release.</param>
        public void Release(AudioSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            source.Stop();
            source.clip = null;
            pool.Release(source);
        }

        /// <summary>Creates additional inactive audio sources.</summary>
        /// <param name="count">The number of instances to create.</param>
        public void Prewarm(int count) => pool.Prewarm(count);

        /// <summary>Creates up to the requested number of instances in one step.</summary>
        /// <param name="count">The maximum number of instances to create.</param>
        /// <returns>The number of instances created.</returns>
        public int PrewarmStep(int count) => pool.PrewarmStep(count);

        /// <summary>Destroys inactive audio sources.</summary>
        public void Clear() => pool.Clear();

        /// <summary>Destroys all audio sources and prevents further use.</summary>
        public void Dispose() => pool.Dispose();
    }
}
