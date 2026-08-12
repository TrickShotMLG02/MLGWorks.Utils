using System;
using UnityEngine;
using MLGWorks.Utils.Helpers.Unity;

namespace MLGWorks.Utils.Patterns.Singletons
{
    /// <summary>Provides a generic base class for implementing the singleton pattern in Unity.</summary>
    /// <typeparam name="T">The type of the singleton class inheriting from <see cref="Singleton{T}"/>.</typeparam>
    /// <remarks>
    /// This class ensures that only one instance of the specified type exists in the scene.
    /// The first instance found in the scene becomes the singleton. Duplicate components
    /// are destroyed, and the instance is cleared when its component is destroyed.
    /// This class does not create a GameObject or persist it across scene loads.
    /// </remarks>
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        /// <summary>Gets the singleton instance of the specified type.</summary>
        /// <remarks>
        /// If the instance has not been registered during <see cref="Awake"/>, this property
        /// searches the scene for the first matching component. It never creates a new GameObject.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when no instance exists in the scene.</exception>
        public static T Instance
        {
            get
            {
                T current = SingletonState<T>.Current;
                if (IsMissing(current))
                {
                    SingletonState<T>.Clear(current);
                    current = null;
                }

                if (current == null)
                {
                    current = FindFirstObjectByType<T>();
                    if (current != null)
                    {
                        SingletonState<T>.TrySet(current);
                    }
                }

                if (IsMissing(current))
                {
                    throw new InvalidOperationException($"No {typeof(T)} found in scene.");
                }

                return current;
            }
        }

        /// <summary>Checks both regular null values and Unity's destroyed-object null state.</summary>
        /// <param name="value">The candidate singleton instance.</param>
        /// <returns><see langword="true"/> when the instance is unavailable.</returns>
        private static bool IsMissing(T value)
        {
            return value == null || value is UnityEngine.Object unityObject && unityObject == null;
        }

        /// <summary>Finds or assigns the first instance and destroys duplicate components.</summary>
        /// <remarks>
        /// Inactive scene objects are included in the initial search so that a disabled
        /// singleton component can still own the singleton state.
        /// </remarks>
        private void SingletonSetup()
        {
            T current = SingletonState<T>.Current;
            if (IsMissing(current))
            {
                SingletonState<T>.Clear(current);
                current = null;
            }

            if (current == null)
            {
                var instances = FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.InstanceID);
                current = instances.Length > 0 ? instances[0] : null;

                if (current != null)
                {
                    SingletonState<T>.TrySet(current);
                }
            }

            if (SingletonState<T>.Current == null)
            {
                SingletonState<T>.TrySet(this as T);
            }
            else if (!ReferenceEquals(SingletonState<T>.Current, this))
            {
                Debug.LogWarning($"Duplicate instance of {typeof(T)} destroyed.");
                gameObject.SafeDestroy();
            }
        }

        /// <summary>Clears the shared state when this component owns the singleton.</summary>
        private void SingletonReset()
        {
            SingletonState<T>.Clear(this as T);
        }

        /// <summary>Initializes the singleton state for this component.</summary>
        /// <remarks>
        /// Derived classes overriding this method should call <c>base.Awake()</c> so that
        /// duplicate handling and singleton setup still occur.
        /// </remarks>
        protected virtual void Awake()
        {
            SingletonSetup();
        }

        /// <summary>Clears this component from the shared singleton state when it is destroyed.</summary>
        /// <remarks>
        /// Derived classes overriding this method should call <c>base.OnDestroy()</c> so that
        /// a later scene instance can become the singleton.
        /// </remarks>
        protected virtual void OnDestroy()
        {
            SingletonReset();
        }
    }
}
