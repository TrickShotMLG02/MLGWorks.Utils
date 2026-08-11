using System;

namespace MLGWorks.Utils.Helpers.Collections.Editor
{
    /// <summary>Associates an editor drawer extension with a serializable collection type.</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class CollectionDrawerExtensionAttribute : Attribute
    {
        /// <summary>Gets the collection type handled by the extension.</summary>
        public Type CollectionType { get; }

        /// <summary>Creates a collection drawer extension registration.</summary>
        /// <param name="collectionType">The collection type to handle.</param>
        public CollectionDrawerExtensionAttribute(Type collectionType)
        {
            CollectionType = collectionType ?? throw new ArgumentNullException(nameof(collectionType));
        }
    }
}
