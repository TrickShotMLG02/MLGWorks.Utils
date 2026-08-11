using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using MLGWorks.Utils.Helpers.SceneManagement.Core;
using MLGWorks.Utils.Helpers.SceneManagement.Loading.Operations;
using MLGWorks.Utils.Helpers.SceneManagement.Transitions;

namespace MLGWorks.Utils.Helpers.SceneManagement.Loading
{
    /// <summary>Provides validated synchronous and asynchronous scene-loading operations.</summary>
    /// <remarks>All methods must be called from Unity's main thread.</remarks>
    public static class SceneLoader
    {
        /// <summary>Loads a scene in single mode and optionally makes it active.</summary>
        /// <param name="scene">The scene reference to load.</param>
        /// <param name="setActiveScene">Whether to make the loaded scene active.</param>
        /// <exception cref="ArgumentNullException">Thrown when scene is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when scene has no valid runtime data.</exception>
        public static void Load(SceneReference scene, bool setActiveScene = true)
        {
            Load(scene, LoadSceneMode.Single, setActiveScene);
        }

        /// <summary>Loads a scene synchronously with the selected Unity load mode.</summary>
        /// <param name="scene">The scene reference to load.</param>
        /// <param name="mode">The Unity load mode.</param>
        /// <param name="setActiveScene">Whether to make the loaded scene active.</param>
        /// <exception cref="ArgumentNullException">Thrown when scene is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when scene has no valid runtime data.</exception>
        public static void Load(SceneReference scene, LoadSceneMode mode, bool setActiveScene = true)
        {
            Validate(scene);
            SceneManager.LoadScene(scene.ScenePath, mode);

            if (setActiveScene)
            {
                SetActiveIfLoaded(scene.ScenePath);
            }
        }

        /// <summary>Starts an asynchronous single-mode scene load.</summary>
        /// <param name="scene">The scene reference to load.</param>
        /// <param name="setActiveScene">Whether to make the loaded scene active after completion.</param>
        /// <param name="onCompleted">Optional callback receiving the loaded scene.</param>
        /// <returns>A handle exposing progress, completion, and wrapper cancellation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when scene is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when scene has no valid runtime data.</exception>
        public static SceneLoadOperation LoadAsync(
            SceneReference scene,
            bool setActiveScene = true,
            Action<Scene> onCompleted = null,
            Action<float> onProgress = null,
            Action<Exception> onFailed = null)
        {
            return LoadAsync(scene, LoadSceneMode.Single, setActiveScene, onCompleted, onProgress, onFailed);
        }

        /// <summary>Starts an asynchronous scene load with the selected Unity load mode.</summary>
        /// <param name="scene">The scene reference to load.</param>
        /// <param name="mode">The Unity load mode.</param>
        /// <param name="setActiveScene">Whether to make the loaded scene active after completion.</param>
        /// <param name="onCompleted">Optional callback receiving the loaded scene.</param>
        /// <returns>A handle exposing progress, completion, and wrapper cancellation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when scene is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when scene has no valid runtime data.</exception>
        public static SceneLoadOperation LoadAsync(
            SceneReference scene,
            LoadSceneMode mode,
            bool setActiveScene = true,
            Action<Scene> onCompleted = null,
            Action<float> onProgress = null,
            Action<Exception> onFailed = null)
        {
            Validate(scene);
            AsyncOperation operation = SceneManager.LoadSceneAsync(scene.ScenePath, mode);
            if (operation == null)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.ScenePath}' could not be loaded. Ensure it is included in Build Settings.");
            }

            return new SceneLoadOperation(operation, scene.ScenePath, setActiveScene, onCompleted, onProgress, onFailed);
        }

        /// <summary>Gets whether a referenced scene is currently loaded.</summary>
        /// <param name="scene">The scene reference to inspect.</param>
        /// <returns>True when the referenced scene is loaded; otherwise false.</returns>
        public static bool IsLoaded(SceneReference scene)
        {
            if (scene == null || !scene.IsValid)
            {
                return false;
            }

            Scene loadedScene = SceneManager.GetSceneByPath(scene.ScenePath);
            return loadedScene.IsValid() && loadedScene.isLoaded;
        }

        /// <summary>Gets the loaded Unity scene represented by a reference.</summary>
        /// <param name="scene">The scene reference to resolve.</param>
        /// <returns>The loaded Unity scene.</returns>
        /// <exception cref="ArgumentNullException">Thrown when scene is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the scene is invalid or not loaded.</exception>
        public static Scene GetLoadedScene(SceneReference scene)
        {
            if (scene == null)
            {
                throw new ArgumentNullException(nameof(scene));
            }

            if (!scene.IsValid)
            {
                throw new InvalidOperationException($"Scene reference '{scene.name}' is invalid.");
            }

            Scene loadedScene = SceneManager.GetSceneByPath(scene.ScenePath);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                throw new InvalidOperationException($"Scene '{scene.ScenePath}' is not loaded.");
            }

            return loadedScene;
        }

        /// <summary>Starts unloading a loaded scene without waiting for completion.</summary>
        /// <param name="scene">The loaded scene reference to unload.</param>
        /// <exception cref="ArgumentNullException">Thrown when scene is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the scene is invalid or not loaded.</exception>
        public static void Unload(SceneReference scene)
        {
            UnloadAsync(scene);
        }

        /// <summary>Starts asynchronously unloading a loaded scene.</summary>
        /// <param name="scene">The loaded scene reference to unload.</param>
        /// <param name="onCompleted">Optional callback invoked after unloading.</param>
        /// <returns>A handle for the asynchronous unload operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when scene is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the scene is invalid, not loaded, or Unity cannot start unloading it.</exception>
        public static SceneUnloadOperation UnloadAsync(SceneReference scene, Action onCompleted = null)
        {
            Scene loadedScene = GetLoadedScene(scene);
            AsyncOperation operation = SceneManager.UnloadSceneAsync(loadedScene);
            if (operation == null)
            {
                throw new InvalidOperationException($"Scene '{scene.ScenePath}' could not be unloaded.");
            }

            return new SceneUnloadOperation(operation, onCompleted);
        }

        /// <summary>Loads a scene between an outgoing and incoming transition.</summary>
        /// <param name="scene">The scene reference to load.</param>
        /// <param name="transition">The transition implementation to execute.</param>
        /// <param name="mode">The Unity load mode.</param>
        /// <param name="setActiveScene">Whether to activate the scene after loading.</param>
        /// <param name="onCompleted">Optional completion callback.</param>
        /// <param name="onProgress">Optional load progress callback.</param>
        /// <param name="onFailed">Optional failure callback.</param>
        /// <returns>A handle for the entire transition and load.</returns>
        /// <exception cref="ArgumentNullException">Thrown when scene or transition is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when scene data is invalid or unavailable.</exception>
        public static SceneTransitionOperation LoadWithTransition(
            SceneReference scene,
            ISceneTransition transition,
            LoadSceneMode mode = LoadSceneMode.Single,
            bool setActiveScene = true,
            Action<Scene> onCompleted = null,
            Action<float> onProgress = null,
            Action<Exception> onFailed = null)
        {
            if (scene == null)
            {
                throw new ArgumentNullException(nameof(scene));
            }

            if (transition == null)
            {
                throw new ArgumentNullException(nameof(transition));
            }

            return new SceneTransitionOperation(
                scene,
                transition,
                mode,
                setActiveScene,
                onCompleted,
                onProgress,
                onFailed);
        }

        /// <summary>Checks whether a scene reference is valid and present in Build Settings.</summary>
        /// <param name="scene">The scene reference to inspect.</param>
        /// <returns>True when the reference can be loaded by Unity's scene manager.</returns>
        public static bool CanLoad(SceneReference scene) => scene != null && scene.IsValid && scene.IsInBuildSettings;

        internal static void Validate(SceneReference scene)
        {
            if (scene == null)
            {
                throw new ArgumentNullException(nameof(scene));
            }

            if (!scene.IsValid)
            {
                throw new InvalidOperationException(
                    $"Scene reference '{scene.name}' does not contain a valid scene path and name.");
            }

            if (!scene.IsInBuildSettings)
            {
                throw new InvalidOperationException(
                    $"Scene '{scene.ScenePath}' is not included in Build Settings.");
            }
        }

        private static void SetActiveIfLoaded(string scenePath)
        {
            Scene loadedScene = SceneManager.GetSceneByPath(scenePath);
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                SceneManager.SetActiveScene(loadedScene);
            }
        }
    }
}
