using System;

namespace MLGWorks.Utils.Helpers
{
    /// <summary>
    /// A utility class for handling simple countdown timers with pause/resume functionality.
    /// </summary>
    public class Timer
    {
        private float _elapsed;

        /// <summary>
        /// Gets the total duration of the timer in seconds.
        /// </summary>
        public float Duration { get; }

        /// <summary>
        /// Gets the elapsed time in seconds since the timer started.
        /// </summary>
        public float Elapsed => _elapsed;

        /// <summary>
        /// Gets whether the timer is currently running and not paused.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets whether the timer is currently paused.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Gets whether the timer has finished.
        /// </summary>
        public bool IsFinished => !IsRunning && _elapsed >= Duration;

        /// <summary>
        /// Occurs when the timer finishes.
        /// </summary>
        public event Action OnFinished;

        /// <summary>
        /// Initializes a new instance of the <see cref="Timer"/> class.
        /// </summary>
        /// <param name="duration">The duration of the timer in seconds.</param>
        public Timer(float duration)
        {
            Duration = duration;
            _elapsed = 0f;
            IsRunning = false;
            IsPaused = false;
        }

        /// <summary>
        /// Starts or restarts the timer from zero.
        /// </summary>
        public void Start()
        {
            _elapsed = 0f;
            IsRunning = true;
            IsPaused = false;
        }

        /// <summary>
        /// Stops the timer and resets its elapsed time.
        /// </summary>
        public void Reset()
        {
            _elapsed = 0f;
            IsRunning = false;
            IsPaused = false;
        }

        /// <summary>
        /// Pauses the timer, freezing its progress.
        /// </summary>
        public void Pause()
        {
            if (IsRunning)
            {
                IsPaused = true;
            }
        }

        /// <summary>
        /// Resumes the timer if it was previously paused.
        /// </summary>
        public void Resume()
        {
            if (IsRunning && IsPaused)
            {
                IsPaused = false;
            }
        }

        /// <summary>
        /// Updates the timer’s elapsed time. Call this once per frame with deltaTime.
        /// </summary>
        /// <param name="deltaTime">The time to advance the timer by, in seconds.</param>
        public void Update(float deltaTime)
        {
            if (!IsRunning || IsPaused)
            {
                return;
            }

            _elapsed += deltaTime;

            if (_elapsed >= Duration)
            {
                _elapsed = Duration;
                IsRunning = false;
                IsPaused = false;
                OnFinished?.Invoke();
            }
        }
    }
}
