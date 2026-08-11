using System;
using UnityEngine.SceneManagement;
using MLGWorks.Utils.Helpers.SceneManagement.Core;
using MLGWorks.Utils.Helpers.SceneManagement.Loading;
using MLGWorks.Utils.Helpers.SceneManagement.Loading.Operations;

namespace MLGWorks.Utils.Helpers.SceneManagement.Transitions
{
    /// <summary>Coordinates an outgoing transition, scene load, and incoming transition.</summary>
    public sealed class SceneTransitionOperation : IDisposable
    {
        private readonly SceneReference scene;
        private readonly ISceneTransition transition;
        private readonly bool setActiveScene;
        private readonly LoadSceneMode mode;
        private readonly Action<Scene> onCompleted;
        private readonly Action<float> onProgress;
        private readonly Action<Exception> onFailed;
        private SceneLoadOperation loadOperation;
        private Exception failure;
        private bool canceled;
        private bool done;

        internal SceneTransitionOperation(
            SceneReference scene,
            ISceneTransition transition,
            LoadSceneMode mode,
            bool setActiveScene,
            Action<Scene> onCompleted,
            Action<float> onProgress,
            Action<Exception> onFailed)
        {
            this.scene = scene ?? throw new ArgumentNullException(nameof(scene));
            this.transition = transition ?? throw new ArgumentNullException(nameof(transition));
            this.mode = mode;
            this.setActiveScene = setActiveScene;
            this.onCompleted = onCompleted;
            this.onProgress = onProgress;
            this.onFailed = onFailed;

            SceneLoader.Validate(scene);
            try
            {
                transition.PlayOut(scene, StartLoad);
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        /// <summary>Gets the underlying scene load operation after the outgoing transition completes.</summary>
        public SceneLoadOperation LoadOperation => loadOperation;

        /// <summary>Gets the current load progress, or zero while the outgoing transition is playing.</summary>
        public float Progress => loadOperation?.Progress ?? 0f;

        /// <summary>Gets whether the complete transition operation has finished.</summary>
        public bool IsDone => done;

        /// <summary>Gets whether the operation was canceled.</summary>
        public bool IsCanceled => canceled;

        /// <summary>Gets whether the operation failed.</summary>
        public bool IsFailed => failure != null;

        /// <summary>Gets the failure, or null when no failure occurred.</summary>
        public Exception Failure => failure;

        /// <summary>Stops callbacks and cancels the wrapper for the underlying load.</summary>
        public void Cancel()
        {
            if (done || canceled)
            {
                return;
            }

            canceled = true;
            loadOperation?.Cancel();
        }

        /// <summary>Releases the underlying load callback subscription.</summary>
        public void Dispose()
        {
            canceled = true;
            loadOperation?.Dispose();
        }

        private void StartLoad()
        {
            if (canceled)
            {
                return;
            }

            try
            {
                loadOperation = SceneLoader.LoadAsync(
                    scene,
                    mode,
                    setActiveScene,
                    HandleLoaded,
                    onProgress,
                    Fail);
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private void HandleLoaded(Scene loadedScene)
        {
            if (canceled)
            {
                return;
            }

            try
            {
                transition.PlayIn(scene, () =>
                {
                    if (canceled)
                    {
                        return;
                    }

                    done = true;
                    onCompleted?.Invoke(loadedScene);
                });
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private void Fail(Exception exception)
        {
            if (done || canceled || failure != null)
            {
                return;
            }

            failure = exception ?? new InvalidOperationException("The scene transition failed.");
            onFailed?.Invoke(failure);
        }
    }
}
