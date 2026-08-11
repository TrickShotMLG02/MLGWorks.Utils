using System;
using System.Collections.Generic;
using UnityEditor;

namespace MLGWorks.Utils.Helpers.Collections.Editor
{
    /// <summary>Discovers and manages editor drawer extensions for serializable collections.</summary>
    public static class CollectionDrawerRegistry
    {
        private static readonly List<ICollectionDrawerExtension> extensions = new();
        private static bool initialized;

        /// <summary>Registers an extension for the current editor session.</summary>
        /// <param name="extension">The extension to register.</param>
        /// <exception cref="ArgumentNullException">Thrown when extension is null.</exception>
        public static void Register(ICollectionDrawerExtension extension)
        {
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            EnsureInitialized();
            if (!extensions.Contains(extension))
            {
                extensions.Add(extension);
            }
        }

        /// <summary>Unregisters an extension for the current editor session.</summary>
        /// <param name="extension">The extension to unregister.</param>
        /// <returns>True when the extension was registered; otherwise false.</returns>
        public static bool Unregister(ICollectionDrawerExtension extension)
        {
            EnsureInitialized();
            return extension != null && extensions.Remove(extension);
        }

        /// <summary>Finds the first extension that handles a collection type.</summary>
        /// <param name="collectionType">The reflected collection type.</param>
        /// <returns>A matching extension, or null.</returns>
        public static ICollectionDrawerExtension Find(Type collectionType)
        {
            EnsureInitialized();
            if (collectionType == null)
            {
                return null;
            }

            for (int index = extensions.Count - 1; index >= 0; index--)
            {
                if (extensions[index].CanDraw(collectionType))
                {
                    return extensions[index];
                }
            }

            return null;
        }

        /// <summary>Clears registered extensions and discovers attribute-based extensions again.</summary>
        public static void Refresh()
        {
            extensions.Clear();
            initialized = false;
            EnsureInitialized();
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            foreach (Type extensionType in TypeCache.GetTypesDerivedFrom<ICollectionDrawerExtension>())
            {
                var attribute = (CollectionDrawerExtensionAttribute)Attribute.GetCustomAttribute(
                    extensionType,
                    typeof(CollectionDrawerExtensionAttribute));
                if (attribute == null || extensionType.IsAbstract || extensionType.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                if (Activator.CreateInstance(extensionType) is ICollectionDrawerExtension extension)
                {
                    extensions.Add(extension);
                }
            }
        }
    }
}
