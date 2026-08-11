using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MLGWorks.Utils.Helpers.SceneManagement.Loading.Operations
{
    /// <summary>
    /// Represents an asynchronous scene load and exposes progress and completion state.
    /// </summary>
    /// <remarks>
    /// Unity does not provide a true cancellation API for <see cref="AsyncOperation"/> scene
    /// loads. <see cref="Cancel"/> therefore cancels this wrapper's callbacks and active-scene
    /// handling, while the underlying Unity load continues to completion.
    /// </remarks>
    public sealed class SceneLoadOperation : IDisposable
    {
        private readonly AsyncOperation operation;
        private readonly Action<Scene> onCompleted;
        private readonly bool setActiveScene;
        private readonly string scenePath;
        private readonly Action<float> onProgress;
        private readonly Action<Exception> onFailed;
        private bool callbackAttached;
        private bool canceled;
        private float lastReportedProgress = -1f;
        private Exception failure;

        internal SceneLoadOperation(
            AsyncOperation operation,
            string scenePath,
            bool setActiveScene,
            Action<Scene> onCompleted,
            Action<float> onProgress,
            Action<Exception> onFailed)
        {
            this.operation = operation ?? throw new ArgumentNullException(nameof(operation));
            this.scenePath = scenePath ?? throw new ArgumentNullException(nameof(scenePath));
            this.setActiveScene = setActiveScene;
            this.onCompleted = onCompleted;
            this.onProgress = onProgress;
            this.onFailed = onFailed;
            operation.completed += HandleCompleted;
            callbackAttached = true;
        }

        /// <summary>Gets Unity's underlying asynchronous operation.</summary>
        public AsyncOperation Operation => operation;

        /// <summary>Gets the normalized load progress from 0 to 1.</summary>
        public float Progress => Mathf.Clamp01(operation.progress);

        /// <summary>Gets whether Unity has completed loading the scene.</summary>
        public bool IsDone => operation.isDone;

        /// <summary>Gets whether this wrapper has been canceled.</summary>
        public bool IsCanceled => canceled;

        /// <summary>Gets whether the load failed before producing a valid loaded scene.</summary>
        public bool IsFailed => failure != null;

        /// <summary>Gets the load failure, or null when no failure occurred.</summary>
        public Exception Failure => failure;

        /// <summary>
        /// Stops this wrapper from invoking callbacks or changing the active scene.
        /// </summary>
        /// <remarks>The underlying Unity scene load cannot be canceled and will continue.</remarks>
        public void Cancel()
        {
            if (operation.isDone || canceled)
            {
                return;
            }

            canceled = true;
            DetachCallback();
        }

        /// <summary>Releases the completion callback subscription.</summary>
        public void Dispose()
        {
            canceled = true;
            DetachCallback();
        }

        private void HandleCompleted(AsyncOperation completedOperation)
        {
            ReportProgress();
            DetachCallback();
            if (canceled)
            {
                return;
            }

            Scene scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                failure = new InvalidOperationException(
                    $"Scene '{scenePath}' finished loading but could not be found as a loaded scene.");
                onFailed?.Invoke(failure);
                return;
            }

            if (setActiveScene)
            {
                SceneManager.SetActiveScene(scene);
            }

            onCompleted?.Invoke(scene);
        }

        internal void ReportProgress()
        {
            float progress = Progress;
            if (onProgress == null || progress <= lastReportedProgress)
            {
                return;
            }

            lastReportedProgress = progress;
            onProgress(progress);
        }

        private void DetachCallback()
        {
            if (!callbackAttached)
            {
                return;
            }

            operation.completed -= HandleCompleted;
            callbackAttached = false;
        }
    }
}
