#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MLGWorks.Utils.Helpers.SceneManagement.Core;

namespace MLGWorks.Utils.Helpers.SceneManagement.Editor
{
    /// <summary>Provides editor tooling for inspecting scene references and Build Settings.</summary>
    public sealed class SceneReferenceEditorWindow : EditorWindow
    {
        private readonly Dictionary<SceneReference, bool> foldouts = new();
        private List<SceneReference> references = new();
        private Vector2 scrollPosition;

        /// <summary>Opens the scene reference management window.</summary>
        [MenuItem("Tools/MLGWorks/Scene Reference Manager")]
        public static void ShowWindow()
        {
            GetWindow<SceneReferenceEditorWindow>("Scene References").Show();
        }

        private void OnEnable() => Refresh();

        private void OnFocus() => Refresh();

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Scene References", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "SceneReference assets store build-safe scene paths. This window edits their metadata and manages only scenes explicitly represented by a reference.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh")) Refresh();
                if (GUILayout.Button("Validate")) ValidateReferences();
                if (GUILayout.Button("Add Valid Scenes")) AddValidScenesToBuildSettings();
            }

            EditorGUILayout.Space();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            foreach (SceneReference reference in references)
            {
                DrawReference(reference);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawReference(SceneReference reference)
        {
            if (reference == null)
            {
                return;
            }

            if (!foldouts.ContainsKey(reference))
            {
                foldouts[reference] = false;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                string label = reference.IsValid
                    ? $"{reference.DisplayName} ({reference.SceneName})"
                    : $"{reference.name} (Invalid)";
                foldouts[reference] = EditorGUILayout.Foldout(foldouts[reference], label, true);
                if (!foldouts[reference])
                {
                    return;
                }

                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField("Reference", reference, typeof(SceneReference), false);
#if UNITY_EDITOR
                    EditorGUILayout.ObjectField("Scene Asset", reference.SceneAsset, typeof(SceneAsset), false);
#endif
                    EditorGUILayout.TextField("Scene Path", reference.ScenePath);
                    EditorGUILayout.TextField("Scene Name", reference.SceneName);
                }

                SerializedObject serialized = new(reference);
                serialized.Update();
                EditorGUILayout.PropertyField(serialized.FindProperty("displayName"), new GUIContent("Display Name"));
                EditorGUILayout.PropertyField(serialized.FindProperty("notes"));
                bool changed = serialized.ApplyModifiedProperties();

                if (!reference.IsValid)
                {
                    EditorGUILayout.HelpBox("Assign a scene asset and save the asset before loading or adding it to Build Settings.", MessageType.Warning);
                }
                else if (!reference.IsInBuildSettings)
                {
                    EditorGUILayout.HelpBox("This scene is not included in Build Settings. Runtime loading will fail until it is added.", MessageType.Warning);
                }

                using (new EditorGUI.DisabledScope(!reference.IsValid))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Open Single"))
                        {
                            EditorSceneManager.OpenScene(reference.ScenePath, OpenSceneMode.Single);
                        }

                        if (GUILayout.Button("Open Additive"))
                        {
                            EditorSceneManager.OpenScene(reference.ScenePath, OpenSceneMode.Additive);
                        }
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (IsInBuildSettings(reference.ScenePath))
                        {
                            if (GUILayout.Button("Remove from Build Settings"))
                            {
                                RemoveFromBuildSettings(reference.ScenePath);
                            }
                        }
                        else if (GUILayout.Button("Add to Build Settings"))
                        {
                            AddToBuildSettings(reference.ScenePath);
                        }
                    }
                }

                if (changed)
                {
                    EditorUtility.SetDirty(reference);
                }
            }
        }

        private void Refresh()
        {
            references = AssetDatabase.FindAssets("t:SceneReference")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SceneReference>)
                .Where(reference => reference != null)
                .OrderBy(reference => reference.DisplayName)
                .ToList();

            HashSet<SceneReference> current = references.ToHashSet();
            foreach (SceneReference reference in foldouts.Keys.Where(reference => !current.Contains(reference)).ToArray())
            {
                foldouts.Remove(reference);
            }
        }

        private void ValidateReferences()
        {
            List<SceneReference> invalid = references.Where(reference => !reference.IsValid).ToList();
            List<SceneReference> missingFromBuild = references
                .Where(reference => reference.IsValid && !reference.IsInBuildSettings)
                .ToList();
            List<string> duplicatePaths = references
                .Where(reference => reference.IsValid)
                .GroupBy(reference => reference.ScenePath)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            HashSet<string> referencedPaths = references
                .Where(reference => reference.IsValid)
                .Select(reference => reference.ScenePath)
                .ToHashSet();
            List<string> unreferencedBuildScenes = EditorBuildSettings.scenes
                .Select(scene => scene.path)
                .Where(path => !referencedPaths.Contains(path))
                .ToList();

            if (invalid.Count == 0 && missingFromBuild.Count == 0 &&
                duplicatePaths.Count == 0 && unreferencedBuildScenes.Count == 0)
            {
                Debug.Log($"Validated {references.Count} scene references successfully.");
                return;
            }

            foreach (SceneReference reference in invalid)
            {
                Debug.LogWarning($"Scene reference '{AssetDatabase.GetAssetPath(reference)}' is invalid.", reference);
            }

            foreach (SceneReference reference in missingFromBuild)
            {
                Debug.LogWarning(
                    $"Scene reference '{AssetDatabase.GetAssetPath(reference)}' is missing from Build Settings.",
                    reference);
            }

            foreach (string path in duplicatePaths)
            {
                Debug.LogWarning($"Multiple SceneReference assets point to '{path}'.");
            }

            foreach (string path in unreferencedBuildScenes)
            {
                Debug.LogWarning($"Build Settings contains scene '{path}' without a SceneReference.");
            }
        }

        private void AddValidScenesToBuildSettings()
        {
            foreach (SceneReference reference in references.Where(reference => reference.IsValid))
            {
                AddToBuildSettings(reference.ScenePath);
            }
        }

        private static bool IsInBuildSettings(string scenePath)
        {
            return !string.IsNullOrEmpty(scenePath) &&
                   EditorBuildSettings.scenes.Any(scene => scene.path == scenePath);
        }

        private static void AddToBuildSettings(string scenePath)
        {
            if (IsInBuildSettings(scenePath))
            {
                return;
            }

            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void RemoveFromBuildSettings(string scenePath)
        {
            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Where(scene => scene.path != scenePath)
                .ToArray();
        }
    }
}
#endif
