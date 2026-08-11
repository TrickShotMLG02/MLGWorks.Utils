using System;

namespace MLGWorks.Utils.Helpers.Timing
{
    /// <summary>
    /// Represents a reusable countdown that gates an operation until its duration expires.
    /// </summary>
    /// <remarks>
    /// The cooldown starts ready. Call <see cref="Update"/> with elapsed time and
    /// <see cref="TryConsume"/> when the guarded operation is attempted.
    /// </remarks>
    public sealed class Cooldown
    {
        private float remaining;

        /// <summary>Gets the configured cooldown duration in seconds.</summary>
        public float Duration { get; }

        /// <summary>Gets the remaining cooldown time in seconds.</summary>
        public float Remaining => remaining;

        /// <summary>Gets whether the cooldown can currently be consumed.</summary>
        public bool IsReady => remaining <= 0f;

        /// <summary>Gets whether the cooldown is currently active.</summary>
        public bool IsRunning => remaining > 0f;

        /// <summary>
        /// Creates a cooldown that starts in the ready state.
        /// </summary>
        /// <param name="duration">The cooldown duration in seconds.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when duration is negative, NaN, or infinite.</exception>
        public Cooldown(float duration)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            Duration = duration;
        }

        /// <summary>Starts or restarts the cooldown at its configured duration.</summary>
        public void Start() => remaining = Duration;

        /// <summary>Resets the cooldown to the ready state.</summary>
        public void Reset() => remaining = 0f;

        /// <summary>
        /// Advances the cooldown by elapsed time, clamping at zero.
        /// </summary>
        /// <param name="deltaTime">The elapsed time in seconds.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when deltaTime is negative, NaN, or infinite.</exception>
        public void Update(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            remaining = Math.Max(0f, remaining - deltaTime);
        }

        /// <summary>
        /// Consumes the cooldown if ready and starts it again.
        /// </summary>
        /// <returns>True when the operation was allowed; otherwise false.</returns>
        public bool TryConsume()
        {
            if (!IsReady)
            {
                return false;
            }

            Start();
            return true;
        }
    }
}
