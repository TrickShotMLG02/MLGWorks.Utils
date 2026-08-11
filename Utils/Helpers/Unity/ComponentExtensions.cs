using System;
using UnityEngine;

namespace MLGWorks.Utils.Helpers.Unity
{
    /// <summary>Provides component lookup and creation extensions for Unity objects.</summary>
    public static class ComponentExtensions
    {
        /// <summary>Tries to find a component on a GameObject or one of its children.</summary>
        /// <typeparam name="T">The component type to find.</typeparam>
        /// <param name="gameObject">The GameObject to search.</param>
        /// <param name="component">The found component, or null when none exists.</param>
        /// <param name="includeInactive">Whether inactive objects should be searched.</param>
        /// <returns>True when a component was found; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when gameObject is null.</exception>
        public static bool TryGetComponentInChildren<T>(
            this GameObject gameObject,
            out T component,
            bool includeInactive = true) where T : Component
        {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));

            component = gameObject.GetComponentInChildren<T>(includeInactive);
            return component != null;
        }

        /// <summary>Tries to find a component on a GameObject or one of its parents.</summary>
        /// <typeparam name="T">The component type to find.</typeparam>
        /// <param name="gameObject">The GameObject whose hierarchy should be searched.</param>
        /// <param name="component">The found component, or null when none exists.</param>
        /// <param name="includeInactive">Whether inactive parents should be searched.</param>
        /// <returns>True when a component was found; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when gameObject is null.</exception>
        public static bool TryGetComponentInParent<T>(
            this GameObject gameObject,
            out T component,
            bool includeInactive = true) where T : Component
        {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));

            component = gameObject.GetComponentInParent<T>(includeInactive);
            return component != null;
        }

        /// <summary>
        /// Finds a component in the object hierarchy and adds it to the root when missing.
        /// </summary>
        /// <typeparam name="T">The component type to find or add.</typeparam>
        /// <param name="gameObject">The root GameObject to inspect.</param>
        /// <param name="includeInactive">Whether inactive descendants should be searched.</param>
        /// <returns>The existing or newly added component.</returns>
        /// <exception cref="ArgumentNullException">Thrown when gameObject is null.</exception>
        public static T GetOrAddComponentInChildren<T>(
            this GameObject gameObject,
            bool includeInactive = true) where T : Component
        {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));
            return gameObject.TryGetComponentInChildren(out T component, includeInactive)
                ? component
                : gameObject.AddComponent<T>();
        }
    }
}
