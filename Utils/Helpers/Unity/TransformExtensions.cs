using System;
using UnityEngine;

namespace MLGWorks.Utils.Helpers.Unity
{
    /// <summary>Provides transform and hierarchy extensions for Unity objects.</summary>
    public static class TransformExtensions
    {
        /// <summary>Resets local position, rotation, and scale when they are not already default.</summary>
        /// <param name="transform">The Transform to reset.</param>
        /// <returns>True when at least one value changed; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when transform is null.</exception>
        public static bool ResetLocalTransform(this Transform transform)
        {
            if (transform == null) throw new ArgumentNullException(nameof(transform));
            bool changed = transform.localPosition != Vector3.zero ||
                           transform.localRotation != Quaternion.identity ||
                           transform.localScale != Vector3.one;
            if (!changed) return false;

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            return true;
        }

        /// <summary>Sets a GameObject layer on itself and all descendants.</summary>
        /// <param name="gameObject">The root GameObject whose hierarchy should be updated.</param>
        /// <param name="layer">The layer index from 0 through 31.</param>
        /// <returns>The number of GameObjects whose layer changed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when gameObject is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when layer is outside 0 through 31.</exception>
        public static int SetLayerRecursively(this GameObject gameObject, int layer)
        {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));
            if (layer < 0 || layer > 31) throw new ArgumentOutOfRangeException(nameof(layer));

            return SetLayer(gameObject.transform, layer);
        }

        /// <summary>Sets active state on a GameObject and all descendants.</summary>
        /// <param name="gameObject">The root GameObject whose hierarchy should be updated.</param>
        /// <param name="active">The desired active state.</param>
        /// <returns>The number of GameObjects whose active state changed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when gameObject is null.</exception>
        public static int SetActiveHierarchy(this GameObject gameObject, bool active)
        {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));

            return SetActive(gameObject.transform, active);
        }

        /// <summary>Destroys all direct children of a Transform.</summary>
        /// <param name="transform">The Transform whose children should be destroyed.</param>
        /// <param name="immediate">Whether to use DestroyImmediate instead of Destroy.</param>
        /// <returns>The number of children scheduled or destroyed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when transform is null.</exception>
        public static int DestroyChildren(this Transform transform, bool immediate = false)
        {
            if (transform == null) throw new ArgumentNullException(nameof(transform));

            int count = transform.childCount;
            for (int i = count - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (immediate || !Application.isPlaying)
                {
                    UnityEngine.Object.DestroyImmediate(child);
                }
                else
                {
                    UnityEngine.Object.Destroy(child);
                }
            }

            return count;
        }

        /// <summary>Finds a child by Unity hierarchy path.</summary>
        /// <param name="transform">The Transform from which the path is resolved.</param>
        /// <param name="path">A direct-child name or slash-separated hierarchy path.</param>
        /// <param name="includeInactive">Whether inactive results may be returned.</param>
        /// <returns>The matching child, or null when none exists.</returns>
        /// <exception cref="ArgumentNullException">Thrown when transform or path is null.</exception>
        public static Transform FindChildPath(
            this Transform transform,
            string path,
            bool includeInactive = true)
        {
            if (transform == null) throw new ArgumentNullException(nameof(transform));
            if (path == null) throw new ArgumentNullException(nameof(path));

            Transform result = transform.Find(path);
            return result != null && (includeInactive || result.gameObject.activeInHierarchy)
                ? result
                : null;
        }

        private static int SetLayer(Transform transform, int layer)
        {
            int changed = 0;
            if (transform.gameObject.layer != layer)
            {
                transform.gameObject.layer = layer;
                changed++;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                changed += SetLayer(transform.GetChild(i), layer);
            }

            return changed;
        }

        private static int SetActive(Transform transform, bool active)
        {
            int changed = transform.gameObject.SetActiveIfChanged(active) ? 1 : 0;

            for (int i = 0; i < transform.childCount; i++)
            {
                changed += SetActive(transform.GetChild(i), active);
            }

            return changed;
        }
    }
}
