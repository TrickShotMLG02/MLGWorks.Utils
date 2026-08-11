using System;
using UnityEngine;

namespace MLGWorks.Utils.Helpers.SceneManagement.Loading.Operations
{
    /// <summary>Represents an asynchronous scene unload operation.</summary>
    public sealed class SceneUnloadOperation : IDisposable
    {
        private readonly AsyncOperation operation;
        private readonly Action onCompleted;
        private bool callbackAttached;

        internal SceneUnloadOperation(
            AsyncOperation operation,
            Action onCompleted)
        {
            this.operation = operation ?? throw new ArgumentNullException(nameof(operation));
            this.onCompleted = onCompleted;
            operation.completed += HandleCompleted;
            callbackAttached = true;
        }

        /// <summary>Gets Unity's underlying asynchronous operation.</summary>
        public AsyncOperation Operation => operation;

        /// <summary>Gets the normalized unload progress from 0 to 1.</summary>
        public float Progress => Mathf.Clamp01(operation.progress);

        /// <summary>Gets whether Unity has completed unloading the scene.</summary>
        public bool IsDone => operation.isDone;

        /// <summary>Releases the completion callback subscription.</summary>
        public void Dispose() => DetachCallback();

        private void HandleCompleted(AsyncOperation completedOperation)
        {
            DetachCallback();
            onCompleted?.Invoke();
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
