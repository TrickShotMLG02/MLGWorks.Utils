using MLGWorks.Utils.Helpers.Collections;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MLGWorks.Utils.Tests.Helpers.Collections
{
    public sealed class SerializableCollectionTests
    {
        [Test]
        public void Dictionary_StartsEmptyAndSupportsBasicLookup()
        {
            var dictionary = new StringIntDictionary();

            Assert.AreEqual(0, dictionary.Count);
            Assert.IsTrue(dictionary.TryAdd("score", 10));
            Assert.AreEqual(10, dictionary["score"]);
            Assert.IsTrue(dictionary.ContainsKey("score"));
            Assert.IsFalse(dictionary.TryAdd("score", 20));
        }

        [Test]
        public void Dictionary_IndexerUpdatesSerializedRuntimeValue()
        {
            var dictionary = new StringIntDictionary();
            dictionary.Add("value", 1);
            dictionary["value"] = 2;

            Assert.AreEqual(2, dictionary["value"]);
            Assert.AreEqual(1, GetSerializedEntries(dictionary).Count);
        }

        [Test]
        public void Dictionary_AddRejectsNullAndDuplicateKeys()
        {
            var dictionary = new StringIntDictionary();
            Assert.Throws<ArgumentNullException>(() => dictionary.Add(null, 1));
            dictionary.Add("key", 1);
            Assert.Throws<ArgumentException>(() => dictionary.Add("key", 2));
        }

        [Test]
        public void Dictionary_RemoveAndClearUpdateCount()
        {
            var dictionary = new StringIntDictionary();
            dictionary.Add("a", 1);
            dictionary.Add("b", 2);

            Assert.IsTrue(dictionary.Remove("a"));
            Assert.IsFalse(dictionary.Remove("missing"));
            dictionary.Clear();

            Assert.AreEqual(0, dictionary.Count);
        }

        [Test]
        public void Dictionary_MissingIndexerThrows()
        {
            var dictionary = new StringIntDictionary();

            Assert.Throws<KeyNotFoundException>(() => _ = dictionary["missing"]);
        }

        [Test]
        public void Dictionary_RejectPolicyReportsDuplicateSerializedKeys()
        {
            var dictionary = CreateDictionaryWithEntries(
                new SerializableKeyValuePair<string, int>("key", 1),
                new SerializableKeyValuePair<string, int>("key", 2));
            dictionary.DuplicateKeyPolicy = DuplicateKeyPolicy.Reject;
            dictionary.Rebuild();

            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual(1, dictionary["key"]);
            Assert.IsFalse(dictionary.LastValidation.IsValid);
            Assert.AreEqual(CollectionValidationIssueType.DuplicateKey, dictionary.LastValidation.Issues[0].Type);
        }

        [Test]
        public void Dictionary_KeepLastPolicyUsesLastDuplicateValue()
        {
            var dictionary = CreateDictionaryWithEntries(
                new SerializableKeyValuePair<string, int>("key", 1),
                new SerializableKeyValuePair<string, int>("key", 2));
            dictionary.DuplicateKeyPolicy = DuplicateKeyPolicy.KeepLast;
            dictionary.Rebuild();

            Assert.AreEqual(2, dictionary["key"]);
            Assert.AreEqual(1, dictionary.LastValidation.Issues.Count);
        }

        [Test]
        public void Dictionary_ValidationFindsNullAndMalformedEntries()
        {
            var dictionary = CreateDictionaryWithEntries(null, new SerializableKeyValuePair<string, int>(null, 1));

            CollectionValidationResult result = dictionary.Validate();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(2, result.Issues.Count);
            Assert.AreEqual(CollectionValidationIssueType.InvalidEntry, result.Issues[0].Type);
            Assert.AreEqual(CollectionValidationIssueType.NullValue, result.Issues[1].Type);
        }

        [Test]
        public void Dictionary_SerializationCallbackRebuildsRuntimeState()
        {
            var dictionary = CreateDictionaryWithEntries(new SerializableKeyValuePair<string, int>("key", 4));

            Assert.AreEqual(4, dictionary["key"]);
            ((ISerializationCallbackReceiver)dictionary).OnBeforeSerialize();
            ((ISerializationCallbackReceiver)dictionary).OnAfterDeserialize();

            Assert.AreEqual(1, dictionary.Count);
            Assert.AreEqual(4, dictionary["key"]);
        }

        [Test]
        public void HashSet_AddRemoveAndDuplicateOperationsArePredictable()
        {
            var set = new StringHashSet();

            Assert.IsTrue(set.Add("one"));
            Assert.IsFalse(set.Add("one"));
            Assert.IsTrue(set.Contains("one"));
            Assert.IsTrue(set.Remove("one"));
            Assert.IsFalse(set.Contains("one"));
            Assert.IsFalse(set.Remove("one"));
        }

        [Test]
        public void HashSet_ValidationFindsNullAndDuplicateItems()
        {
            var set = new StringHashSet();
            SetSerializedItems(set, null, "item", "item");

            CollectionValidationResult result = set.Validate();

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(2, result.Issues.Count);
            Assert.AreEqual(CollectionValidationIssueType.NullValue, result.Issues[0].Type);
            Assert.AreEqual(CollectionValidationIssueType.DuplicateKey, result.Issues[1].Type);
        }

        [Test]
        public void Lookup_AllowsMultipleValuesPerKey()
        {
            var lookup = new StringLookup();
            lookup.Add("tag", "red");
            lookup.Add("tag", "large");

            Assert.AreEqual(1, lookup.Count);
            Assert.IsTrue(lookup.TryGetValues("tag", out IReadOnlyList<string> values));
            CollectionAssert.AreEqual(new[] { "red", "large" }, values);
        }

        [Test]
        public void Lookup_MissingKeyReturnsEmptyValues()
        {
            var lookup = new StringLookup();

            Assert.IsFalse(lookup.TryGetValues("missing", out IReadOnlyList<string> values));
            Assert.IsEmpty(values);
            Assert.IsEmpty(lookup.GetValues("missing"));
        }

        [Test]
        public void Lookup_RemoveDeletesAllValuesForKey()
        {
            var lookup = new StringLookup();
            lookup.Add("tag", "red");
            lookup.Add("tag", "large");

            Assert.IsTrue(lookup.Remove("tag"));
            Assert.AreEqual(0, lookup.Count);
            Assert.IsFalse(lookup.Remove("tag"));
        }

        [Test]
        public void Lookup_NullKeysAreRejected()
        {
            var lookup = new StringLookup();

            Assert.Throws<ArgumentNullException>(() => lookup.Add(null, "value"));
        }

        [Test]
        public void Dictionary_PreservesSerializedOrderAfterMutations()
        {
            var dictionary = new StringIntDictionary();
            dictionary.AddRange(new[]
            {
                new KeyValuePair<string, int>("first", 1),
                new KeyValuePair<string, int>("second", 2),
                new KeyValuePair<string, int>("third", 3)
            });
            dictionary["second"] = 20;

            CollectionAssert.AreEqual(new[] { "first", "second", "third" }, GetDictionaryKeys(dictionary));
            CollectionAssert.AreEqual(new[] { 1, 20, 3 }, GetDictionaryValues(dictionary));
        }

        [Test]
        public void Dictionary_AddRangeIsAtomicWhenInputContainsDuplicate()
        {
            var dictionary = new StringIntDictionary();
            dictionary.Add("existing", 1);

            Assert.Throws<ArgumentException>(() => dictionary.AddRange(new[]
            {
                new KeyValuePair<string, int>("new", 2),
                new KeyValuePair<string, int>("new", 3)
            }));

            Assert.AreEqual(1, dictionary.Count);
            Assert.IsFalse(dictionary.ContainsKey("new"));
        }

        [Test]
        public void Dictionary_SetValuesReplacesCollectionInInputOrder()
        {
            var dictionary = new StringIntDictionary();
            dictionary.Add("old", 1);
            dictionary.SetValues(new[]
            {
                new KeyValuePair<string, int>("b", 2),
                new KeyValuePair<string, int>("a", 1)
            });

            CollectionAssert.AreEqual(new[] { "b", "a" }, GetDictionaryKeys(dictionary));
            Assert.IsFalse(dictionary.ContainsKey("old"));
        }

        [Test]
        public void Dictionary_BulkFilteringAndReadOnlyInterfaceWork()
        {
            var dictionary = new StringIntDictionary();
            dictionary.AddRange(new[]
            {
                new KeyValuePair<string, int>("keep", 1),
                new KeyValuePair<string, int>("remove", 2)
            });
            IReadOnlySerializableDictionary<string, int> readOnly = dictionary;

            Assert.IsTrue(readOnly.ContainsKey("keep"));
            Assert.IsTrue(dictionary.ContainsAll(new[] { "keep", "remove" }));
            Assert.AreEqual(1, dictionary.RemoveWhere((key, _) => key == "remove"));
            Assert.IsFalse(dictionary.ContainsKey("remove"));
        }

        [Test]
        public void HashSet_PreservesSerializedOrderAndSupportsBulkOperations()
        {
            var set = new StringHashSet();
            set.AddRange(new[] { "first", "second", "third" });

            CollectionAssert.AreEqual(new[] { "first", "second", "third" }, set.SerializedItems);
            Assert.IsTrue(set.ContainsAll(new[] { "first", "third" }));
            Assert.AreEqual(1, set.RemoveWhere(value => value == "second"));
            CollectionAssert.AreEqual(new[] { "first", "third" }, set.SerializedItems);
        }

        [Test]
        public void HashSet_AddRangeIsAtomicWhenInputContainsDuplicate()
        {
            var set = new StringHashSet();
            set.Add("existing");

            Assert.Throws<ArgumentException>(() => set.AddRange(new[] { "new", "new" }));

            CollectionAssert.AreEqual(new[] { "existing" }, set.SerializedItems);
        }

        [Test]
        public void HashSet_ReadOnlyInterfaceExposesMembershipWithoutMutation()
        {
            var set = new StringHashSet();
            set.Add("item");
            IReadOnlySerializableSet<string> readOnly = set;

            Assert.IsTrue(readOnly.Contains("item"));
            CollectionAssert.AreEqual(new[] { "item" }, readOnly.Items);
        }

        [Test]
        public void Lookup_PreservesOrderAndCanReplaceValuesForOneKey()
        {
            var lookup = new StringLookup();
            lookup.AddRange(new[]
            {
                new KeyValuePair<string, string>("first", "one"),
                new KeyValuePair<string, string>("second", "two"),
                new KeyValuePair<string, string>("first", "old")
            });
            lookup.SetValues("first", new[] { "new", "latest" });

            CollectionAssert.AreEqual(new[] { "new", "latest" }, lookup.GetValues("first"));
            Assert.AreEqual("two", lookup.Entries[2].Value);
        }

        [Test]
        public void Lookup_BulkFilteringAndReadOnlyInterfaceWork()
        {
            var lookup = new StringLookup();
            lookup.Add("item", "keep");
            lookup.Add("item", "remove");
            lookup.Add("other", "keep");
            IReadOnlySerializableLookup<string, string> readOnly = lookup;

            Assert.IsTrue(readOnly.ContainsKey("item"));
            Assert.AreEqual(2, lookup.RemoveWhere((_, value) => value == "remove" || value == "other"));
            Assert.IsTrue(lookup.ContainsAll(new[] { "item" }));
            Assert.IsFalse(lookup.ContainsKey("other"));
        }

        [Test]
        public void Dictionary_CustomComparerControlsRuntimeKeyEquality()
        {
            var dictionary = new CaseInsensitiveDictionary();
            dictionary.Add("Player", 10);

            Assert.IsTrue(dictionary.ContainsKey("player"));
            Assert.AreEqual(10, dictionary["PLAYER"]);
            Assert.IsFalse(dictionary.TryAdd("pLaYeR", 20));
        }

        [Test]
        public void Dictionary_NotificationsReportChangesAndRebuilds()
        {
            var dictionary = new StringIntDictionary();
            int changed = 0;
            int added = 0;
            int removed = 0;
            int rebuilt = 0;
            dictionary.Changed += () => changed++;
            dictionary.ItemAdded += (_, _) => added++;
            dictionary.ItemRemoved += (_, _) => removed++;
            dictionary.CollectionRebuilt += () => rebuilt++;

            dictionary.Add("key", 1);
            dictionary.Remove("key");
            dictionary.Rebuild();

            Assert.AreEqual(2, changed);
            Assert.AreEqual(1, added);
            Assert.AreEqual(1, removed);
            Assert.AreEqual(1, rebuilt);
        }

        [Test]
        public void Validation_UsesWarningForDictionaryDuplicatesAndInformationForLookupRepeats()
        {
            var dictionary = CreateDictionaryWithEntries(
                new SerializableKeyValuePair<string, int>("key", 1),
                new SerializableKeyValuePair<string, int>("key", 2));
            CollectionValidationResult dictionaryResult = dictionary.Validate();

            var lookup = new StringLookup();
            lookup.Add("key", "first");
            lookup.Add("key", "second");
            CollectionValidationResult lookupResult = lookup.Validate();

            Assert.IsTrue(dictionaryResult.HasWarnings);
            Assert.AreEqual(CollectionValidationSeverity.Warning, dictionaryResult.Issues[0].Severity);
            Assert.IsTrue(lookupResult.HasInformation);
            Assert.IsTrue(lookupResult.IsValid);
            Assert.AreEqual(CollectionValidationSeverity.Information, lookupResult.Issues[0].Severity);
        }

        private static string[] GetDictionaryKeys(StringIntDictionary dictionary)
        {
            var keys = new string[dictionary.Entries.Count];
            for (int index = 0; index < keys.Length; index++)
            {
                keys[index] = dictionary.Entries[index].Key;
            }

            return keys;
        }

        private static int[] GetDictionaryValues(StringIntDictionary dictionary)
        {
            var values = new int[dictionary.Entries.Count];
            for (int index = 0; index < values.Length; index++)
            {
                values[index] = dictionary.Entries[index].Value;
            }

            return values;
        }

        private static StringIntDictionary CreateDictionaryWithEntries(
            params SerializableKeyValuePair<string, int>[] entries)
        {
            var dictionary = new StringIntDictionary();
            GetSerializedEntries(dictionary).AddRange(entries);
            return dictionary;
        }

        private static List<SerializableKeyValuePair<string, int>> GetSerializedEntries(StringIntDictionary dictionary)
        {
            return (List<SerializableKeyValuePair<string, int>>)typeof(SerializableDictionary<string, int>)
                .GetField("entries", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(dictionary);
        }

        private static void SetSerializedItems(StringHashSet set, params string[] values)
        {
            var items = (List<string>)typeof(SerializableHashSet<string>)
                .GetField("items", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(set);
            items.AddRange(values);
        }

        [Serializable]
        private sealed class StringIntDictionary : SerializableDictionary<string, int> { }

        [Serializable]
        private sealed class StringHashSet : SerializableHashSet<string> { }

        [Serializable]
        private sealed class StringLookup : SerializableLookup<string, string> { }

        [Serializable]
        private sealed class CaseInsensitiveDictionary : SerializableDictionary<string, int>
        {
            protected override IEqualityComparer<string> CreateComparer() => StringComparer.OrdinalIgnoreCase;
        }
    }
}
