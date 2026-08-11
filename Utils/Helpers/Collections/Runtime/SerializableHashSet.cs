using System;
using System.Collections.Generic;
using UnityEngine;

namespace MLGWorks.Utils.Helpers.Collections
{
    /// <summary>Represents a Unity-serializable set backed by a serialized list.</summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <remarks>Derive a concrete type for Unity inspector serialization.</remarks>
    [Serializable]
    public class SerializableHashSet<T> : SerializableCollectionBase, IReadOnlySerializableSet<T>
    {
        [SerializeField] private List<T> items = new();
        private HashSet<T> set;
        private CollectionValidationResult lastValidation;

        /// <summary>Raised when an item is added.</summary>
        public event Action<T> ItemAdded;

        /// <summary>Raised when an item is removed.</summary>
        public event Action<T> ItemRemoved;

        /// <summary>Gets the number of unique runtime items.</summary>
        public override int Count
        {
            get
            {
                EnsureRuntimeState();
                return set.Count;
            }
        }

        /// <summary>Gets the runtime items.</summary>
        public IEnumerable<T> Items
        {
            get
            {
                EnsureRuntimeState();
                return set;
            }
        }

        /// <summary>Gets the serialized items in inspector order.</summary>
        public IReadOnlyList<T> SerializedItems => items;

        /// <summary>Gets whether an item exists in the set.</summary>
        /// <param name="item">The item to inspect.</param>
        /// <returns>True when item exists; otherwise false.</returns>
        public bool Contains(T item)
        {
            EnsureRuntimeState();
            return set.Contains(item);
        }

        /// <summary>Adds an item to the set.</summary>
        /// <param name="item">The item to add.</param>
        /// <returns>True when item was new; otherwise false.</returns>
        public bool Add(T item)
        {
            EnsureRuntimeState();
            if (!set.Add(item))
            {
                return false;
            }

            items.Add(item);
            ItemAdded?.Invoke(item);
            NotifyChanged();
            return true;
        }

        /// <summary>Removes an item from the set.</summary>
        /// <param name="item">The item to remove.</param>
        /// <returns>True when item was removed; otherwise false.</returns>
        public bool Remove(T item)
        {
            EnsureRuntimeState();
            bool removed = set.Remove(item);
            if (removed)
            {
                for (int index = 0; index < items.Count; index++)
                {
                    if (EqualityComparer<T>.Default.Equals(items[index], item))
                    {
                        items.RemoveAt(index);
                        break;
                    }
                }

                ItemRemoved?.Invoke(item);
                NotifyChanged();
            }

            return removed;
        }

        /// <summary>Removes all items.</summary>
        public void Clear()
        {
            EnsureRuntimeState();
            set.Clear();
            items.Clear();
            NotifyChanged();
        }

        /// <summary>Adds multiple items while preserving input order.</summary>
        /// <param name="values">The items to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when values is null.</exception>
        /// <exception cref="ArgumentException">Thrown when values contains duplicates or existing items.</exception>
        public void AddRange(IEnumerable<T> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            EnsureRuntimeState();
            var pending = new List<T>();
            var unique = new HashSet<T>(set, CreateComparer());
            foreach (T value in values)
            {
                if (!unique.Add(value))
                {
                    throw new ArgumentException($"The set item '{value}' is duplicated.", nameof(values));
                }

                pending.Add(value);
            }

            foreach (T value in pending)
            {
                set.Add(value);
                items.Add(value);
                ItemAdded?.Invoke(value);
            }

            if (pending.Count > 0)
            {
                NotifyChanged();
            }
        }

        /// <summary>Replaces all items while preserving input order.</summary>
        /// <param name="values">The replacement items.</param>
        /// <exception cref="ArgumentNullException">Thrown when values is null.</exception>
        /// <exception cref="ArgumentException">Thrown when values contains duplicates.</exception>
        public void SetValues(IEnumerable<T> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var replacement = new List<T>();
            var unique = new HashSet<T>(CreateComparer());
            foreach (T value in values)
            {
                if (!unique.Add(value))
                {
                    throw new ArgumentException($"The set item '{value}' is duplicated.", nameof(values));
                }

                replacement.Add(value);
            }

            EnsureRuntimeState();
            set.Clear();
            items.Clear();
            foreach (T value in replacement)
            {
                set.Add(value);
                items.Add(value);
                ItemAdded?.Invoke(value);
            }

            NotifyChanged();
        }

        /// <summary>Removes every item matching a predicate.</summary>
        /// <param name="predicate">The predicate applied to each item.</param>
        /// <returns>The number of removed items.</returns>
        /// <exception cref="ArgumentNullException">Thrown when predicate is null.</exception>
        public int RemoveWhere(Func<T, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            EnsureRuntimeState();
            int removedCount = 0;
            for (int index = items.Count - 1; index >= 0; index--)
            {
                if (predicate(items[index]))
                {
                    set.Remove(items[index]);
                    ItemRemoved?.Invoke(items[index]);
                    items.RemoveAt(index);
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                NotifyChanged();
            }

            return removedCount;
        }

        /// <summary>Gets whether all supplied items exist.</summary>
        /// <param name="values">The items to inspect.</param>
        /// <returns>True when every item exists; otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when values is null.</exception>
        public bool ContainsAll(IEnumerable<T> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            EnsureRuntimeState();
            foreach (T value in values)
            {
                if (!set.Contains(value))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Validates serialized items for null values and duplicates.</summary>
        /// <returns>The validation result.</returns>
        public override CollectionValidationResult Validate()
        {
            var issues = new List<CollectionValidationIssue>();
            var seen = new HashSet<T>(CreateComparer());
            for (int index = 0; index < (items?.Count ?? 0); index++)
            {
                T item = items[index];
                if (ReferenceEquals(item, null))
                {
                    issues.Add(new CollectionValidationIssue(
                        CollectionValidationIssueType.NullValue,
                        index,
                        "Set items cannot be null."));
                }
                else if (!seen.Add(item))
                {
                    issues.Add(new CollectionValidationIssue(
                        CollectionValidationIssueType.DuplicateKey,
                        index,
                        $"The set item '{item}' occurs more than once.",
                        CollectionValidationSeverity.Warning));
                }
            }

            return lastValidation = new CollectionValidationResult(issues);
        }

        /// <summary>Gets the latest validation result generated during deserialization.</summary>
        public CollectionValidationResult LastValidation => lastValidation ?? Validate();

        protected override void RebuildRuntimeState()
        {
            set = new HashSet<T>(CreateComparer());
            items ??= new List<T>();
            set.Clear();
            var issues = new List<CollectionValidationIssue>();
            for (int index = 0; index < (items?.Count ?? 0); index++)
            {
                T item = items[index];
                if (ReferenceEquals(item, null))
                {
                    issues.Add(new CollectionValidationIssue(
                        CollectionValidationIssueType.NullValue,
                        index,
                        "Set items cannot be null."));
                    continue;
                }

                if (!set.Add(item))
                {
                    issues.Add(new CollectionValidationIssue(
                        CollectionValidationIssueType.DuplicateKey,
                        index,
                        $"The set item '{item}' occurs more than once.",
                        CollectionValidationSeverity.Warning));
                }
            }

            lastValidation = new CollectionValidationResult(issues);
        }

        protected override void SynchronizeSerializedData()
        {
            items ??= new List<T>();
            // The serialized list is the source of truth and must retain inspector order.
            // Public mutations update both representations directly.
        }

        /// <summary>Creates the equality comparer used by runtime set operations.</summary>
        /// <returns>The comparer for set items.</returns>
        protected virtual IEqualityComparer<T> CreateComparer() => EqualityComparer<T>.Default;
    }
}
