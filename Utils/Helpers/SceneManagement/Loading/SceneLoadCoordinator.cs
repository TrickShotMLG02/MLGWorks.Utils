using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using MLGWorks.Utils.Helpers.SceneManagement.Core;
using MLGWorks.Utils.Helpers.SceneManagement.Loading.Operations;

namespace MLGWorks.Utils.Helpers.SceneManagement.Loading
{
    /// <summary>
    /// Coordinates asynchronous scene loads, duplicate requests, progress callbacks, and cleanup.
    /// </summary>
    /// <remarks>
    /// Create and own one coordinator per gameplay flow. Call <see cref="Tick"/> from that owner's
    /// main-thread update loop to receive progress callbacks. The coordinator intentionally does not
    /// create a hidden Unity object or singleton.
    /// </remarks>
    public sealed class SceneLoadCoordinator : IDisposable
    {
        private readonly Dictionary<string, SceneLoadOperation> activeLoads = new();
        private readonly DuplicateSceneLoadBehavior duplicateBehavior;
        private bool disposed;

        /// <summary>Creates a coordinator with the selected duplicate-load policy.</summary>
        /// <param name="duplicateBehavior">The behavior for an already-loading scene.</param>
        public SceneLoadCoordinator(
            DuplicateSceneLoadBehavior duplicateBehavior = DuplicateSceneLoadBehavior.ReturnExisting)
        {
            this.duplicateBehavior = duplicateBehavior;
        }

        /// <summary>Gets the number of scene loads currently tracked by this coordinator.</summary>
        public int ActiveLoadCount => activeLoads.Count;

        /// <summary>Gets whether a scene is currently being loaded by this coordinator.</summary>
        /// <param name="scene">The scene reference to inspect.</param>
        /// <returns>True when a matching load is in progress.</returns>
        public bool IsLoading(SceneReference scene)
        {
            return scene != null && scene.IsValid && activeLoads.ContainsKey(scene.ScenePath);
        }

        /// <summary>
        /// Starts a coordinated asynchronous scene load.
        /// </summary>
        /// <param name="scene">The scene reference to load.</param>
        /// <param name="mode">The Unity load mode.</param>
        /// <param name="setActiveScene">Whether to activate the scene after loading.</param>
        /// <param name="onCompleted">Optional completion callback.</param>
        /// <param name="onProgress">Optional progress callback, driven by <see cref="Tick"/>.</param>
        /// <param name="onFailed">Optional failure callback.</param>
        /// <returns>The new or existing load operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when scene is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown for invalid scenes or duplicate requests configured to throw.</exception>
        public SceneLoadOperation LoadAsync(
            SceneReference scene,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool setActiveScene = true,
            Action<Scene> onCompleted = null,
            Action<float> onProgress = null,
            Action<Exception> onFailed = null)
        {
            ThrowIfDisposed();
            SceneLoader.Validate(scene);

            if (SceneLoader.IsLoaded(scene))
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.ScenePath}' is already loaded. Unload it before loading it again.");
            }

            if (activeLoads.TryGetValue(scene.ScenePath, out SceneLoadOperation existing))
            {
                if (duplicateBehavior == DuplicateSceneLoadBehavior.Throw)
                {
                    throw new InvalidOperationException(
                        $"Scene '{scene.ScenePath}' is already being loaded.");
                }

                return existing;
            }

            SceneLoadOperation operation = null;
            operation = SceneLoader.LoadAsync(
                scene,
                mode,
                setActiveScene,
                loadedScene =>
                {
                    activeLoads.Remove(scene.ScenePath);
                    onCompleted?.Invoke(loadedScene);
                },
                onProgress,
                failure =>
                {
                    activeLoads.Remove(scene.ScenePath);
                    onFailed?.Invoke(failure);
                });

            activeLoads.Add(scene.ScenePath, operation);
            return operation;
        }

        /// <summary>
        /// Reports current progress for every active operation and removes completed cancellations.
        /// </summary>
        public void Tick()
        {
            ThrowIfDisposed();

            foreach (SceneLoadOperation operation in new List<SceneLoadOperation>(activeLoads.Values))
            {
                operation.ReportProgress();
            }

            foreach (KeyValuePair<string, SceneLoadOperation> pair in new List<KeyValuePair<string, SceneLoadOperation>>(activeLoads))
            {
                if (pair.Value.IsDone)
                {
                    activeLoads.Remove(pair.Key);
                }
            }
        }

        /// <summary>Unloads a loaded scene through the shared scene-loader validation.</summary>
        /// <param name="scene">The loaded scene reference to unload.</param>
        /// <param name="onCompleted">Optional completion callback.</param>
        /// <returns>The asynchronous unload operation.</returns>
        public SceneUnloadOperation UnloadAsync(SceneReference scene, Action onCompleted = null)
        {
            ThrowIfDisposed();
            return SceneLoader.UnloadAsync(scene, onCompleted);
        }

        /// <summary>Cancels wrapper callbacks for all tracked loads and releases their subscriptions.</summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            foreach (SceneLoadOperation operation in activeLoads.Values)
            {
                operation.Dispose();
            }

            activeLoads.Clear();
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SceneLoadCoordinator));
            }
        }
    }
}
