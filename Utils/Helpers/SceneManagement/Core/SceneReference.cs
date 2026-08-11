using System;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif

namespace MLGWorks.Utils.Helpers.SceneManagement.Core
{
    /// <summary>
    /// Stores a build-safe reference to a Unity scene and optional presentation metadata.
    /// </summary>
    /// <remarks>
    /// The scene asset field exists only in the Unity Editor. Its path and name are serialized
    /// so runtime code can use this asset without referencing <c>UnityEditor</c> APIs.
    /// </remarks>
    [CreateAssetMenu(fileName = "SceneReference", menuName = "MLGWorks/Scene Reference")]
    public sealed class SceneReference : ScriptableObject
    {
        [SerializeField] private string scenePath = string.Empty;
        [SerializeField] private string sceneName = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, TextArea] private string notes = string.Empty;

#if UNITY_EDITOR
        [SerializeField] private SceneAsset sceneAsset;
#endif

        /// <summary>Gets the project path of the referenced scene.</summary>
        public string ScenePath => scenePath;

        /// <summary>Gets the file name of the referenced scene without its extension.</summary>
        public string SceneName => sceneName;

        /// <summary>Gets the custom display name, or <see cref="SceneName"/> when none is configured.</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? SceneName : displayName;

        /// <summary>Gets optional notes associated with the scene.</summary>
        public string Notes => notes;

        /// <summary>Gets whether the reference contains the data required for runtime loading.</summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(scenePath) && !string.IsNullOrWhiteSpace(sceneName);

        /// <summary>Gets whether the referenced scene is currently included in Build Settings.</summary>
        public bool IsInBuildSettings => IsValid && SceneUtility.GetBuildIndexByScenePath(ScenePath) >= 0;

#if UNITY_EDITOR
        /// <summary>Gets the editor-only scene asset assigned to this reference.</summary>
        public SceneAsset SceneAsset => sceneAsset;

        /// <summary>Synchronizes serialized runtime data with the assigned scene asset.</summary>
        private void OnValidate()
        {
            if (sceneAsset == null)
            {
                if (!string.IsNullOrEmpty(scenePath) || !string.IsNullOrEmpty(sceneName))
                {
                    scenePath = string.Empty;
                    sceneName = string.Empty;
                    EditorUtility.SetDirty(this);
                }

                return;
            }

            string path = AssetDatabase.GetAssetPath(sceneAsset);
            string name = Path.GetFileNameWithoutExtension(path);
            if (scenePath == path && sceneName == name)
            {
                return;
            }

            scenePath = path;
            sceneName = name;
            EditorUtility.SetDirty(this);
        }
#endif

        /// <summary>
        /// Returns a readable representation of this scene reference, including sensible fallbacks for unassigned assets.
        /// </summary>
        /// <returns>The configured display name and scene path.</returns>
        public override string ToString()
        {
            string label = string.IsNullOrWhiteSpace(DisplayName) ? name : DisplayName;
            string path = string.IsNullOrWhiteSpace(ScenePath) ? "unassigned" : ScenePath;
            return $"{label} ({path})";
        }
    }
}
