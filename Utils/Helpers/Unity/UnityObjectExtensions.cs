using System;
using UnityEngine;

namespace MLGWorks.Utils.Helpers.Unity
{
    /// <summary>Provides small, allocation-free helpers for common Unity object operations.</summary>
    public static class UnityObjectExtensions
    {
        /// <summary>
        /// Gets the requested component from a GameObject or adds it when missing.
        /// </summary>
        /// <typeparam name="T">The component type to find or add.</typeparam>
        /// <param name="gameObject">The GameObject to inspect.</param>
        /// <returns>The existing or newly added component.</returns>
        /// <exception cref="ArgumentNullException">Thrown when gameObject is null.</exception>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));
            return gameObject.TryGetComponent<T>(out T component)
                ? component
                : gameObject.AddComponent<T>();
        }

        /// <summary>
        /// Activates or deactivates a GameObject only when its current state differs.
        /// </summary>
        /// <param name="gameObject">The GameObject whose active state should change.</param>
        /// <param name="active">The desired active state.</param>
        /// <returns>True when SetActive was called; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when gameObject is null.</exception>
        public static bool SetActiveIfChanged(this GameObject gameObject, bool active)
        {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));
            if (gameObject.activeSelf == active) return false;

            gameObject.SetActive(active);
            return true;
        }

        /// <summary>
        /// Changes a Transform's parent only when it differs from the requested parent.
        /// </summary>
        /// <param name="transform">The Transform to reparent.</param>
        /// <param name="parent">The desired parent, or null for the scene root.</param>
        /// <param name="worldPositionStays">Whether the world position should be preserved.</param>
        /// <returns>True when SetParent was called; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when transform is null.</exception>
        public static bool SetParentIfChanged(
            this Transform transform,
            Transform parent,
            bool worldPositionStays = false)
        {
            if (transform == null) throw new ArgumentNullException(nameof(transform));
            if (transform.parent == parent) return false;

            transform.SetParent(parent, worldPositionStays);
            return true;
        }

        /// <summary>
        /// Finds the first matching component in the object or its descendants.
        /// </summary>
        /// <typeparam name="T">The component type to find.</typeparam>
        /// <param name="root">The root GameObject to search.</param>
        /// <param name="includeInactive">Whether inactive descendants should be searched.</param>
        /// <returns>The first matching component, or null when none exists.</returns>
        /// <exception cref="ArgumentNullException">Thrown when root is null.</exception>
        public static T FindInChildren<T>(this GameObject root, bool includeInactive = true)
            where T : Component
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            return root.GetComponentInChildren<T>(includeInactive);
        }

        /// <summary>
        /// Safely destroys a Unity object using the appropriate Play Mode or Edit Mode API.
        /// </summary>
        /// <param name="unityObject">The object to destroy.</param>
        public static void SafeDestroy(this UnityEngine.Object unityObject)
        {
            if (unityObject == null) return;

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(unityObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(unityObject);
            }
        }
    }
}
