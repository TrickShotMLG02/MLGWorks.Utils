using System;
using System.Collections.Generic;
using UnityEngine;

namespace MLGWorks.Utils.DependencyInjection
{
    /// <summary>
    /// A simple static service locator for registering and retrieving service instances
    /// by their types. Designed for lightweight dependency injection and service management.
    /// </summary>
    public static class ServiceLocator
    {
        // Internal dictionary mapping service types to service instances.
        private static readonly Dictionary<Type, object> _services = new();

        /// <summary>
        /// Registers a service instance for the specified type <typeparamref name="T"/>.
        /// If a service is already registered for this type, the registration is ignored
        /// and a warning is logged.
        /// </summary>
        /// <typeparam name="T">The service type to register.</typeparam>
        /// <param name="service">The service instance to register. Must be a class.</param>
        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Service of type {type.FullName} is already registered and will not be overwritten.");
                return;
            }
            _services[type] = service;
        }

        /// <summary>
        /// Registers a service instance for the specified <see cref="Type"/>.
        /// If a service is already registered for this type, the registration is ignored
        /// and a warning is logged.
        /// </summary>
        /// <param name="type">The service type to register.</param>
        /// <param name="instance">The service instance to register.</param>
        public static void Register(Type type, object instance)
        {
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"[ServiceLocator] Service of type {type.FullName} is already registered and will not be overwritten.");
                return;
            }
            _services[type] = instance;
        }

        /// <summary>
        /// Unregisters the service associated with the specified type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The service type to unregister.</typeparam>
        public static void Unregister<T>() where T : class
        {
            _services.Remove(typeof(T));
        }

        /// <summary>
        /// Retrieves the registered service instance of type <typeparamref name="T"/>.
        /// Returns null if no service of that type is registered.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <returns>The registered service instance or null if not found.</returns>
        public static T Get<T>() where T : class
        {
            _services.TryGetValue(typeof(T), out var service);
            return service as T;
        }

        /// <summary>
        /// Tries to retrieve the registered service instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of service to retrieve.</typeparam>
        /// <param name="result">
        /// When this method returns, contains the service instance if found; otherwise, null.
        /// </param>
        /// <returns>True if the service was found; otherwise, false.</returns>
        public static bool TryGet<T>(out T result) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var value) && value is T typed)
            {
                result = typed;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Tries to retrieve the registered service instance for the specified <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The service type to retrieve.</param>
        /// <param name="result">
        /// When this method returns, contains the service instance if found; otherwise, null.
        /// </param>
        /// <returns>True if the service was found; otherwise, false.</returns>
        public static bool TryGet(Type type, out object result)
        {
            return _services.TryGetValue(type, out result);
        }

        public static IEnumerable<object> GetAllServices()
        {
            return _services.Values;
        }

        /// <summary>
        /// Clears all registered services from the locator.
        /// </summary>
        public static void Clear() => _services.Clear();
    }
}
