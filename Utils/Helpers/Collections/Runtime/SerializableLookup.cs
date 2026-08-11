using System;
using System.Collections.Generic;
using UnityEngine;

namespace MLGWorks.Utils.Helpers.Collections
{
    /// <summary>Represents a Unity-serializable one-key-to-many-values lookup.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <remarks>Keys may occur multiple times in serialized data and may have any number of values.</remarks>
    [Serializable]
    public class SerializableLookup<TKey, TValue> : SerializableCollectionBase, IReadOnlySerializableLookup<TKey, TValue>
    {
        [SerializeField] private List<SerializableKeyValuePair<TKey, TValue>> entries = new();

        private Dictionary<TKey, List<TValue>> lookup;
        private CollectionValidationResult lastValidation;

        /// <summary>Raised when a key-value pair is added.</summary>
        public event Action<TKey, TValue> ItemAdded;

        /// <summary>Raised when a key-value pair is removed.</summary>
        public event Action<TKey, TValue> ItemRemoved;

        /// <summary>Gets the number of unique keys.</summary>
        public override int Count
        {
            get
            {
                EnsureRuntimeState();
                return lookup.Count;
            }
        }

        /// <summary>Gets the serialized entries in inspector order.</summary>
        public IReadOnlyList<SerializableKeyValuePair<TKey, TValue>> Entries => entries;

        /// <summary>Gets whether a key has one or more associated values.</summary>
        /// <param name="key">The key to inspect.</param>
        /// <returns>True when key exists; otherwise false.</returns>
        public bool ContainsKey(TKey key)
        {
            EnsureRuntimeState();
            return lookup.ContainsKey(key);
        }

        /// <summary>Adds a value to the values associated with a key.</summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value to associate.</param>
        /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
        public void Add(TKey key, TValue value)
        {
            ValidateKey(key);
            EnsureRuntimeState();
            entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
            if (!lookup.TryGetValue(key, out List<TValue> values))
            {
                values = new List<TValue>();
                lookup.Add(key, values);
            }

            values.Add(value);
            entries.Add(new SerializableKeyValuePair<TKey, TValue>(key, value));
            ItemAdded?.Invoke(key, value);
            NotifyChanged();
        }

        /// <summary>Gets all values associated with a key.</summary>
        /// <param name="key">The key to inspect.</param>
        /// <returns>An immutable snapshot of values, or an empty sequence.</returns>
        public IReadOnlyList<TValue> GetValues(TKey key)
        {
            EnsureRuntimeState();
            return lookup.TryGetValue(key, out List<TValue> values)
                ? values.AsReadOnly()
                : Array.Empty<TValue>();
        }

        /// <summary>Attempts to get values associated with a key.</summary>
        /// <param name="key">The key to inspect.</param>
        /// <param name="values">The associated values, or an empty list.</param>
        /// <returns>True when key exists; otherwise false.</returns>
        public bool TryGetValues(TKey key, out IReadOnlyList<TValue> values)
        {
            EnsureRuntimeState();
            if (!lookup.TryGetValue(key, out List<TValue> found))
            {
                values = Array.Empty<TValue>();
                return false;
            }

            values = found.AsReadOnly();
            return true;
        }

        /// <summary>Removes all values associated with a key.</summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True when key existed; otherwise false.</returns>
        public bool Remove(TKey key)
        {
            EnsureRuntimeState();
            entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
            bool removed = lookup.Remove(key);
            if (removed)
            {
                for (int index = entries.Count - 1; index >= 0; index--)
                {
                    if (entries[index] != null && CreateComparer().Equals(entries[index].Key, key))
                    {
                        ItemRemoved?.Invoke(entries[index].Key, entries[index].Value);
                        entries.RemoveAt(index);
                    }
                }

                NotifyChanged();
            }

            return removed;
        }

        /// <summary>Removes all values from the lookup.</summary>
        public void Clear()
        {
            EnsureRuntimeState();
            entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
            lookup.Clear();
            entries.Clear();
            NotifyChanged();
        }

        /// <summary>Adds multiple key-value pairs while preserving input order.</summary>
        /// <param name="pairs">The pairs to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when pairs is null.</exception>
        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            if (pairs == null)
            {
                throw new ArgumentNullException(nameof(pairs));
            }

            var pending = new List<KeyValuePair<TKey, TValue>>();
            foreach (KeyValuePair<TKey, TValue> pair in pairs)
            {
                ValidateKey(pair.Key);
                pending.Add(pair);
            }

            EnsureRuntimeState();
            entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
            foreach (KeyValuePair<TKey, TValue> pair in pending)
            {
                if (!lookup.TryGetValue(pair.Key, out List<TValue> values))
                {
                    values = new List<TValue>();
                    lookup.Add(pair.Key, values);
                }

                values.Add(pair.Value);
                entries.Add(new SerializableKeyValuePair<TKey, TValue>(pair.Key, pair.Value));
                ItemAdded?.Invoke(pair.Key, pair.Value);
            }

            if (pending.Count > 0)
            {
                NotifyChanged();
            }
        }

        /// <summary>Replaces all values for a key while preserving the key's original position.</summary>
        /// <param name="key">The key to update.</param>
        /// <param name="values">The replacement values.</param>
        /// <exception cref="ArgumentNullException">Thrown when key or values is null.</exception>
        public void SetValues(TKey key, IEnumerable<TValue> values)
        {
            ValidateKey(key);
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var replacement = new List<TValue>(values);
            EnsureRuntimeState();
            entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
            int insertIndex = entries.Count;
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index] != null && CreateComparer().Equals(entries[index].Key, key))
                {
                    insertIndex = index;
                    break;
                }
            }

            Remove(key);
            if (replacement.Count == 0)
            {
                return;
            }

            if (insertIndex > entries.Count)
            {
                insertIndex = entries.Count;
            }

            lookup[key] = replacement;
            for (int offset = 0; offset < replacement.Count; offset++)
            {
                entries.Insert(insertIndex + offset,
                    new SerializableKeyValuePair<TKey, TValue>(key, replacement[offset]));
                ItemAdded?.Invoke(key, replacement[offset]);
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
                    if (lookup.TryGetValue(entry.Key, out List<TValue> values))
                    {
                        values.Remove(entry.Value);
                        if (values.Count == 0)
                        {
                            lookup.Remove(entry.Key);
                        }
                    }

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
                if (!lookup.ContainsKey(key))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Validates serialized entries for null keys.</summary>
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
                        "The serialized lookup entry is null."));
                }
                else if (ReferenceEquals(entry.Key, null))
                {
                    issues.Add(new CollectionValidationIssue(
                        CollectionValidationIssueType.NullValue,
                        index,
                        "Lookup keys cannot be null."));
                }
                else if (!seen.Add(entry.Key))
                {
                    issues.Add(new CollectionValidationIssue(
                        CollectionValidationIssueType.DuplicateKey,
                        index,
                        $"Lookup key '{entry.Key}' intentionally has multiple values.",
                        CollectionValidationSeverity.Information));
                }
            }

            return lastValidation = new CollectionValidationResult(issues);
        }

        /// <summary>Gets the latest validation result generated during deserialization.</summary>
        public CollectionValidationResult LastValidation => lastValidation ?? Validate();

        protected override void RebuildRuntimeState()
        {
            lookup = new Dictionary<TKey, List<TValue>>(CreateComparer());
            entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
            lookup.Clear();
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
                        entry == null ? "The serialized lookup entry is null." : "Lookup keys cannot be null."));
                    continue;
                }

                if (!lookup.TryGetValue(entry.Key, out List<TValue> values))
                {
                    values = new List<TValue>();
                    lookup.Add(entry.Key, values);
                }

                values.Add(entry.Value);
                if (!seen.Add(entry.Key))
                {
                    issues.Add(new CollectionValidationIssue(
                        CollectionValidationIssueType.DuplicateKey,
                        index,
                        $"Lookup key '{entry.Key}' intentionally has multiple values.",
                        CollectionValidationSeverity.Information));
                }
            }

            lastValidation = new CollectionValidationResult(issues);
        }

        protected override void SynchronizeSerializedData()
        {
            entries ??= new List<SerializableKeyValuePair<TKey, TValue>>();
            // The serialized list is the source of truth and must retain inspector order.
            // Public mutations update both representations directly.
        }

        /// <summary>Creates the equality comparer used by runtime lookup operations.</summary>
        /// <returns>The comparer for lookup keys.</returns>
        protected virtual IEqualityComparer<TKey> CreateComparer() => EqualityComparer<TKey>.Default;

        private static void ValidateKey(TKey key)
        {
            if (ReferenceEquals(key, null))
            {
                throw new ArgumentNullException(nameof(key));
            }
        }
    }
}
