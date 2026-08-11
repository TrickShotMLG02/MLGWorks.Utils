using System;

namespace MLGWorks.Utils.Helpers.Collections
{
    /// <summary>Stores a key and value in Unity-serializable form.</summary>
    [Serializable]
    public sealed class SerializableKeyValuePair<TKey, TValue>
    {
        /// <summary>The serialized key.</summary>
        public TKey Key;

        /// <summary>The serialized value.</summary>
        public TValue Value;

        /// <summary>Creates an empty serialized entry.</summary>
        public SerializableKeyValuePair() { }

        /// <summary>Creates a serialized key-value entry.</summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public SerializableKeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}
