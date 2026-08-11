using System;
using UnityEngine;

namespace MLGWorks.Utils.Patterns
{
    /// <summary>
    /// Provides a generic base class for implementing the singleton pattern in Unity.
    /// </summary>
    /// <remarks>This class ensures that only one instance of the specified type <typeparamref name="T"/>
    /// exists in the scene. It provides a thread-safe mechanism to access the singleton instance and manages duplicate
    /// instances by destroying them. Derived classes should override the <see cref="Awake"/> and <see
    /// cref="OnDestroy"/> methods to perform additional setup or cleanup tasks as needed.</remarks>
    /// <typeparam name="T">The type of the singleton class inheriting from <see cref="Singleton{T}"/>.</typeparam>
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        /// <summary>
        /// Holds a single instance of the type <typeparamref name="T"/> for use in singleton patterns.
        /// </summary>
        /// <remarks>This field is intended to store the single instance of a type in a thread-safe
        /// singleton implementation. It is private and static to ensure that only one instance of the type exists and
        /// is accessible only within the containing class.</remarks>
        private static T _instance;

        /// <summary>
        /// Gets the singleton instance of the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <remarks>This property ensures that only one instance of the specified type <typeparamref
        /// name="T"/> is accessed. If the instance does not already exist, it is initialized by locating the first
        /// object of type <typeparamref name="T"/>.</remarks>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();
                }

                if (_instance == null)
                {
                    throw new InvalidOperationException($"No {typeof(T)} found in scene.");
                }

                return _instance;
            }
        }

        /// <summary>
        /// Ensures that only a single instance of the class exists and manages duplicate instances.
        /// </summary>
        /// <remarks>If no instance exists, this method assigns the current object as the singleton
        /// instance. If a duplicate instance is detected, it logs a warning and destroys the duplicate
        /// object.</remarks>
        private void SingletonSetup()
        {
            if (_instance == null)
            {
                // The static field can be unset when objects are created dynamically,
                // especially in Edit Mode tests. Discover an already existing instance
                // before accepting the current component as the singleton.
                var instances = FindObjectsByType<T>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.InstanceID);
                _instance = instances.Length > 0 ? instances[0] : null;
            }

            if (_instance == null)
            {
                _instance = this as T;
            }
            else if (!ReferenceEquals(_instance, this))
            {
                Debug.LogWarning($"Duplicate instance of {typeof(T)} destroyed.");

                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    DestroyImmediate(gameObject);
                }
            }
        }

        /// <summary>
        /// Resets the singleton instance if the current object is the active instance.
        /// </summary>
        /// <remarks>This method sets the singleton instance to <see langword="null"/> if the current
        /// object  is the one currently assigned as the singleton instance. Use this method to release  the singleton
        /// instance when it is no longer needed.</remarks>
        private void SingletonReset()
        {
            if (ReferenceEquals(_instance, this))
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Initializes the instance and performs any necessary setup when the object is awakened.
        /// </summary>
        /// <remarks>This method is typically used to configure the instance as a singleton or perform
        /// other initialization tasks. Override this method in derived classes to extend the setup process.</remarks>
        protected virtual void Awake()
        {
            SingletonSetup();
        }

        /// <summary>
        /// Performs any necessary operations when the object is destroyed.
        /// </summary>
        /// <remarks>This method is typically used to clean up the singleton or perform
        /// other cleanup tasks. Override this method in derived classes to extend the cleanup process.</remarks>
        protected virtual void OnDestroy()
        {
            SingletonReset();
        }
    }
}
