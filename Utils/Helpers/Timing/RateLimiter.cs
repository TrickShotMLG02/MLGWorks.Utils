using System;
using System.Collections.Generic;

namespace MLGWorks.Utils.Helpers.Timing
{
    /// <summary>
    /// Limits the number of operations allowed within a rolling time window.
    /// </summary>
    /// <remarks>
    /// Timestamps passed to <see cref="TryAcquire"/> must be monotonic. The limiter
    /// stores only timestamps within the active window and does not allocate during
    /// normal operation after its initial queue growth.
    /// </remarks>
    public sealed class RateLimiter
    {
        private readonly Queue<double> acquisitionTimes = new();
        private readonly int maxUses;
        private readonly double windowSeconds;
        private double lastTimestamp;
        private bool hasTimestamp;

        /// <summary>Gets the maximum number of uses allowed in the window.</summary>
        public int MaxUses => maxUses;

        /// <summary>Gets the rolling window duration in seconds.</summary>
        public double WindowSeconds => windowSeconds;

        /// <summary>Gets the number of uses remaining at the most recent timestamp.</summary>
        public int RemainingUses => maxUses - acquisitionTimes.Count;

        /// <summary>Gets the number of uses currently recorded in the active window.</summary>
        public int UsedUses => acquisitionTimes.Count;

        /// <summary>
        /// Creates a rolling-window rate limiter.
        /// </summary>
        /// <param name="maxUses">The maximum number of operations allowed per window.</param>
        /// <param name="windowSeconds">The rolling window duration in seconds.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for invalid limits or a non-positive window.</exception>
        public RateLimiter(int maxUses, double windowSeconds)
        {
            if (maxUses <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxUses));
            }

            if (double.IsNaN(windowSeconds) || double.IsInfinity(windowSeconds) || windowSeconds <= 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(windowSeconds));
            }

            this.maxUses = maxUses;
            this.windowSeconds = windowSeconds;
        }

        /// <summary>
        /// Attempts to acquire permission for one operation at a timestamp.
        /// </summary>
        /// <param name="timestamp">The monotonic timestamp in seconds.</param>
        /// <returns>True when the operation is allowed; otherwise false.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when timestamp is NaN, infinite, or moves backwards.</exception>
        public bool TryAcquire(double timestamp)
        {
            ValidateTimestamp(timestamp);
            RemoveExpired(timestamp);

            if (acquisitionTimes.Count >= maxUses)
            {
                return false;
            }

            acquisitionTimes.Enqueue(timestamp);
            return true;
        }

        /// <summary>Clears all recorded uses and returns the limiter to its initial state.</summary>
        public void Reset()
        {
            acquisitionTimes.Clear();
            hasTimestamp = false;
            lastTimestamp = 0d;
        }

        private void ValidateTimestamp(double timestamp)
        {
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp) ||
                (hasTimestamp && timestamp < lastTimestamp))
            {
                throw new ArgumentOutOfRangeException(nameof(timestamp));
            }

            lastTimestamp = timestamp;
            hasTimestamp = true;
        }

        private void RemoveExpired(double timestamp)
        {
            while (acquisitionTimes.Count > 0 && timestamp - acquisitionTimes.Peek() >= windowSeconds)
            {
                acquisitionTimes.Dequeue();
            }
        }
    }
}
