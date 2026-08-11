using System;
using System.Collections.Generic;
using UnityEngine;

namespace MLGWorks.Utils.Helpers.Collections
{
    /// <summary>
    /// Represents a Unity-serializable dictionary backed by a list of key-value entries.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>
    /// Unity serializes the entry list; the dictionary is rebuilt for runtime lookups.
    /// For inspector use, derive a concrete serializable type from this class.
    /// </remarks>
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : SerializableCollectionBase, IReadOnlySerializableDictionary<TKey, TValue>
    {
        [SerializeField] private List<SerializableKeyValuePair<TKey, TValue>> entries = new();
        [SerializeField] private DuplicateKeyPolicy duplicateKeyPolicy = DuplicateKeyPolicy.Reject;

        private Dictionary<TKey, TValue> dictionary;
        private CollectionValidationResult lastValidation;

        /// <summary>Raised when an item is added.</summary>
        public event Action<TKey, TValue> ItemAdded;

        /// <summary>Raised when an item is removed.</summary>
        public event Action<TKey, TValue> ItemRemoved;

        /// <summary>Gets the configured policy for duplicate serialized keys.</summary>
        public DuplicateKeyPolicy DuplicateKeyPolicy
        {
            get => duplicateKeyPolicy;
            set => duplicateKeyPolicy = value;
        }

        /// <summary>Gets the number of unique runtime keys.</summary>
        public override int Count
        {
            get
            {
                EnsureRuntimeState();
                return dictionary.Count;
            }
        }

        /// <summary>Gets the keys in serialized inspector order.</summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                EnsureRuntimeState();
                return GetOrderedKeys();
            }
        }

        /// <summary>Gets the values in serialized inspector order.</summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                EnsureRuntimeState();
                return GetOrderedValues();
            }
        }

        /// <summary>Gets key-value pairs in the serialized inspector order.</summary>
        public IEnumerable<KeyValuePair<TKey, TValue>> Pairs
        {
            get
            {
                EnsureRuntimeState();
                return GetOrderedPairs();
            }
        }

        /// <summary>Gets the serialized entries in inspector order.</summary>
        public IReadOnlyList<SerializableKeyValuePair<TKey, TValue>> Entries => entries;

        /// <summary>Gets or sets the value associated with a key.</summary>
        /// <param name="key">The key to inspect or change.</param>
        /// <returns>The value associated with key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when getting a missing key.</exception>
        public TValue this[TKey key]
        {
            get
            {
                EnsureRuntimeState();
                return dictionary[key];
            }
            set
            {
                ValidateKey(key);
                EnsureRuntimeState();
                entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key] = value;
                    entries[FindEntryIndex(key)].Value = value;
                }
                else
                {
                    dictionary.Add(key, value);
                    entries.Add(new SerializableKeyValuePair<TKey, TValue>(key, value));
                    ItemAdded?.Invoke(key, value);
                }

                NotifyChanged();
            }
        }

        /// <summary>Gets whether the dictionary contains a key.</summary>
        /// <param name="key">The key to inspect.</param>
        /// <returns>True when key exists; otherwise false.</returns>
        public bool ContainsKey(TKey key)
        {
            EnsureRuntimeState();
            return dictionary.ContainsKey(key);
        }

        /// <summary>Attempts to get a value without throwing for missing keys.</summary>
        /// <param name="key">The key to inspect.</param>
        /// <param name="value">The found value, or the default value.</param>
        /// <returns>True when key exists; otherwise false.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            EnsureRuntimeState();
            return dictionary.TryGetValue(key, out value);
        }

        /// <summary>Adds a new key-value pair.</summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
        /// <exception cref="ArgumentException">Thrown when key already exists.</exception>
        public void Add(TKey key, TValue value)
        {
            ValidateKey(key);
            EnsureRuntimeState();
            entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
            dictionary.Add(key, value);
            entries.Add(new SerializableKeyValuePair<TKey, TValue>(key, value));
            ItemAdded?.Invoke(key, value);
            NotifyChanged();
        }

        /// <summary>Attempts to add a key-value pair without throwing for duplicate keys.</summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to add.</param>
        /// <returns>True when the pair was added; otherwise false.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            ValidateKey(key);
            EnsureRuntimeState();
            entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
            if (!dictionary.TryAdd(key, value))
            {
                return false;
            }

            entries.Add(new SerializableKeyValuePair<TKey, TValue>(key, value));
            ItemAdded?.Invoke(key, value);
            NotifyChanged();
            return true;
        }

        /// <summary>Removes a key and its value.</summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True when an entry was removed; otherwise false.</returns>
        public bool Remove(TKey key)
        {
            EnsureRuntimeState();
            entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
            bool removed = dictionary.Remove(key);
            if (removed)
            {
                int entryIndex = FindEntryIndex(key);
                TValue value = entryIndex >= 0 ? entries[entryIndex].Value : default;
                if (entryIndex >= 0)
                {
                    entries.RemoveAt(entryIndex);
                }

                ItemRemoved?.Invoke(key, value);
                NotifyChanged();
            }

            return removed;
        }

        /// <summary>Removes all entries.</summary>
        public void Clear()
        {
            EnsureRuntimeState();
            entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
            dictionary.Clear();
            entries.Clear();
            NotifyChanged();
        }

        /// <summary>Adds multiple key-value pairs while preserving input order.</summary>
        /// <param name="pairs">The pairs to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when pairs is null.</exception>
        /// <exception cref="ArgumentException">Thrown when a key is null or duplicated.</exception>
        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            if (pairs == null)
            {
                throw new ArgumentNullException(nameof(pairs));
            }

            EnsureRuntimeState();
            entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
            var pending = new List<KeyValuePair<TKey, TValue>>();
            var keys = new HashSet<TKey>(dictionary.Keys, CreateComparer());
            foreach (KeyValuePair<TKey, TValue> pair in pairs)
            {
                ValidateKey(pair.Key);
                if (!keys.Add(pair.Key))
                {
                    throw new ArgumentException($"The dictionary key '{pair.Key}' is duplicated.", nameof(pairs));
                }

                pending.Add(pair);
            }

            foreach (KeyValuePair<TKey, TValue> pair in pending)
            {
                dictionary.Add(pair.Key, pair.Value);
                entries.Add(new SerializableKeyValuePair<TKey, TValue>(pair.Key, pair.Value));
                ItemAdded?.Invoke(pair.Key, pair.Value);
            }

            if (pending.Count > 0)
            {
                NotifyChanged();
            }
        }

        /// <summary>Replaces all values with the supplied pairs while preserving input order.</summary>
        /// <param name="pairs">The replacement pairs.</param>
        /// <exception cref="ArgumentNullException">Thrown when pairs is null.</exception>
        /// <exception cref="ArgumentException">Thrown when a key is null or duplicated.</exception>
        public void SetValues(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            if (pairs == null)
            {
                throw new ArgumentNullException(nameof(pairs));
            }

            var replacement = new List<KeyValuePair<TKey, TValue>>();
            var keys = new HashSet<TKey>(CreateComparer());
            foreach (KeyValuePair<TKey, TValue> pair in pairs)
            {
                ValidateKey(pair.Key);
                if (!keys.Add(pair.Key))
                {
                    throw new ArgumentException($"The dictionary key '{pair.Key}' is duplicated.", nameof(pairs));
                }

                replacement.Add(pair);
            }

            EnsureRuntimeState();
            entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
            dictionary.Clear();
            entries.Clear();
            foreach (KeyValuePair<TKey, TValue> pair in replacement)
            {
                dictionary.Add(pair.Key, pair.Value);
                entries.Add(new SerializableKeyValuePair<TKey, TValue>(pair.Key, pair.Value));
                ItemAdded?.Invoke(pair.Key, pair.Value);
            }

            NotifyChanged();
        }

        /// <summary>Removes every pair matching a predicate.</summary>
        /// <param name="predicate">The predicate applied to each pair.</param>
        /// <returns>The number of removed pairs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when predicate is null.</exception>
        public int RemoveWhere(Func<TKey, TValue, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            EnsureRuntimeState();
            entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
            int removedCount = 0;
            for (int index = entries.Count - 1; index >= 0; index--)
            {
                SerializableKeyValuePair<TKey, TValue> entry = entries[index];
                if (entry != null && predicate(entry.Key, entry.Value))
                {
                    dictionary.Remove(entry.Key);
                    entries.RemoveAt(index);
                    ItemRemoved?.Invoke(entry.Key, entry.Value);
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                NotifyChanged();
            }

            return removedCount;
        }

        /// <summary>Gets whether all supplied keys exist.</summary>
        /// <param name="keys">The keys to inspect.</param>
        /// <returns>True when every key exists; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when keys is null.</exception>
        public bool ContainsAll(IEnumerable<TKey> keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            EnsureRuntimeState();
            foreach (TKey key in keys)
            {
                if (!dictionary.ContainsKey(key))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Validates the serialized entries and returns all detected issues.</summary>
        /// <returns>The validation result.</returns>
        public override CollectionValidationResult Validate()
        {
            var issues = new List<CollectionValidationIssue>();
            var seen = new HashSet<TKey>(CreateComparer());
            for (int index = 0; index < (entries?.Count ?? 0); index++)
            {
                SerializableKeyValuePair<TKey, TValue> entry = entries[index];
                if (entry == null)
                {
                    issues.Add(new CollectionValidationIssue(
                        CollectionValidationIssueType.InvalidEntry,
                        index,
                        "The serialized dictionary entry is null."));
                    continue;
                }

                if (ReferenceEquals(entry.Key, null))
                {
                    issues.Add(new CollectionValidationIssue(
                        CollectionValidationIssueType.NullValue,
                        index,
                        "Dictionary keys cannot be null."));
                    continue;
                }

                if (!seen.Add(entry.Key))
                {
                    issues.Add(new CollectionValidationIssue(
                        CollectionValidationIssueType.DuplicateKey,
                        index,
                        $"The dictionary key '{entry.Key}' occurs more than once.",
                        CollectionValidationSeverity.Warning));
                }
            }

            return lastValidation = new CollectionValidationResult(issues);
        }

        /// <summary>Gets the latest validation result generated during deserialization.</summary>
        public CollectionValidationResult LastValidation => lastValidation ?? Validate();

        protected override void RebuildRuntimeState()
        {
            dictionary = new Dictionary<TKey, TValue>(CreateComparer());
            dictionary.Clear();
            var issues = new List<CollectionValidationIssue>();
            var seen = new HashSet<TKey>(CreateComparer());

            for (int index = 0; index < (entries?.Count ?? 0); index++)
            {
                SerializableKeyValuePair<TKey, TValue> entry = entries[index];
                if (entry == null || ReferenceEquals(entry.Key, null))
                {
                    issues.Add(new CollectionValidationIssue(
                        entry == null ? CollectionValidationIssueType.InvalidEntry : CollectionValidationIssueType.NullValue,
                        index,
                        entry == null ? "The serialized dictionary entry is null." : "Dictionary keys cannot be null."));
                    continue;
                }

                if (!seen.Add(entry.Key))
                {
                    issues.Add(new CollectionValidationIssue(
                        CollectionValidationIssueType.DuplicateKey,
                        index,
                        $"The dictionary key '{entry.Key}' occurs more than once.",
                        CollectionValidationSeverity.Warning));

                    if (duplicateKeyPolicy == DuplicateKeyPolicy.KeepFirst || duplicateKeyPolicy == DuplicateKeyPolicy.Reject)
                    {
                        continue;
                    }

                    dictionary[entry.Key] = entry.Value;
                    continue;
                }

                dictionary.Add(entry.Key, entry.Value);
            }

            lastValidation = new CollectionValidationResult(issues);
        }

        protected override void SynchronizeSerializedData()
        {
            dictionary ??= new Dictionary<TKey, TValue>();
            entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
            // The serialized list is the source of truth and must retain inspector order.
            // Public mutations update both representations directly.
        }

        /// <summary>Creates the equality comparer used by runtime dictionary operations.</summary>
        /// <returns>The comparer for dictionary keys.</returns>
        protected virtual IEqualityComparer<TKey> CreateComparer() => EqualityComparer<TKey>.Default;

        private IEnumerable<TKey> GetOrderedKeys()
        {
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index] != null && dictionary.ContainsKey(entries[index].Key))
                {
                    yield return entries[index].Key;
                }
            }
        }

        private IEnumerable<TValue> GetOrderedValues()
        {
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index] != null && dictionary.TryGetValue(entries[index].Key, out TValue value))
                {
                    yield return value;
                }
            }
        }

        private IEnumerable<KeyValuePair<TKey, TValue>> GetOrderedPairs()
        {
            for (int index = 0; index < entries.Count; index++)
            {
                SerializableKeyValuePair<TKey, TValue> entry = entries[index];
                if (entry != null && dictionary.TryGetValue(entry.Key, out TValue value))
                {
                    yield return new KeyValuePair<TKey, TValue>(entry.Key, value);
                }
            }
        }

        private int FindEntryIndex(TKey key)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index] != null && EqualityComparer<TKey>.Default.Equals(entries[index].Key, key))
                {
                    return index;
                }
            }

            return -1;
        }

        private static void ValidateKey(TKey key)
        {
            if (ReferenceEquals(key, null))
            {
                throw new ArgumentNullException(nameof(key));
            }
        }
    }
}
