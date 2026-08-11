using System.Collections.Generic;

namespace MLGWorks.Utils.Helpers.Collections
{
    /// <summary>Exposes common read-only behavior for serialized collections.</summary>
    public interface IReadOnlySerializableCollection
    {
        /// <summary>Gets the number of runtime entries.</summary>
        int Count { get; }

        /// <summary>Validates the serialized collection data.</summary>
        /// <returns>The validation result.</returns>
        CollectionValidationResult Validate();
    }

    /// <summary>Exposes read-only dictionary operations for a serializable dictionary.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public interface IReadOnlySerializableDictionary<TKey, TValue> : IReadOnlySerializableCollection
    {
        /// <summary>Gets whether a key exists.</summary>
        bool ContainsKey(TKey key);

        /// <summary>Attempts to retrieve a value.</summary>
        bool TryGetValue(TKey key, out TValue value);

        /// <summary>Enumerates key-value pairs in serialized order.</summary>
        IEnumerable<KeyValuePair<TKey, TValue>> Pairs { get; }
    }

    /// <summary>Exposes read-only lookup operations for a serializable one-to-many collection.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    public interface IReadOnlySerializableLookup<TKey, TValue> : IReadOnlySerializableCollection
    {
        /// <summary>Gets whether a key exists.</summary>
        bool ContainsKey(TKey key);

        /// <summary>Attempts to retrieve values for a key.</summary>
        bool TryGetValues(TKey key, out IReadOnlyList<TValue> values);
    }

    /// <summary>Exposes read-only set operations for a serializable hash set.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    public interface IReadOnlySerializableSet<T> : IReadOnlySerializableCollection
    {
        /// <summary>Gets whether an item exists.</summary>
        bool Contains(T item);

        /// <summary>Enumerates unique items.</summary>
        IEnumerable<T> Items { get; }
    }

    /// <summary>Exposes runtime rebuilding and validation for serialized collections.</summary>
    public interface ISerializableCollection : IReadOnlySerializableCollection
    {
        /// <summary>Rebuilds the runtime representation from serialized data.</summary>
        void Rebuild();
    }
}
