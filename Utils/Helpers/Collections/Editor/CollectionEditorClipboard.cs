using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MLGWorks.Utils.Helpers.Collections.Editor
{
    /// <summary>Provides editor-session copy, paste, and preset operations for serialized collections.</summary>
    internal static class CollectionEditorClipboard
    {
        private sealed class Snapshot
        {
            public UnityEngine.Object SourceObject;
            public string PropertyPath;
            public Type CollectionType;
        }

        private static Snapshot clipboard;
        private static readonly Dictionary<Type, Snapshot> presets = new();

        /// <summary>Copies a collection into the editor-session clipboard.</summary>
        public static void Copy(SerializedProperty collection, Type collectionType)
        {
            clipboard = CreateSnapshot(collection, collectionType);
        }

        /// <summary>Pastes the editor-session clipboard into a collection.</summary>
        /// <returns>True when a compatible snapshot was pasted.</returns>
        public static bool Paste(SerializedProperty collection, Type collectionType)
        {
            return PasteSnapshot(clipboard, collection, collectionType);
        }

        /// <summary>Saves a collection as the current in-memory preset for its type.</summary>
        public static void SavePreset(SerializedProperty collection, Type collectionType)
        {
            presets[collectionType] = CreateSnapshot(collection, collectionType);
        }

        /// <summary>Loads the current in-memory preset for a collection type.</summary>
        /// <returns>True when a compatible preset was loaded.</returns>
        public static bool LoadPreset(SerializedProperty collection, Type collectionType)
        {
            return presets.TryGetValue(collectionType, out Snapshot snapshot) &&
                   PasteSnapshot(snapshot, collection, collectionType);
        }

        /// <summary>Duplicates the current serialized entries in place.</summary>
        public static void Duplicate(SerializedProperty collection)
        {
            int originalCount = collection.arraySize;
            collection.arraySize += originalCount;
            for (int index = 0; index < originalCount; index++)
            {
                SerializedPropertyCopyUtility.Copy(
                    collection.GetArrayElementAtIndex(index),
                    collection.GetArrayElementAtIndex(originalCount + index));
            }
        }

        private static Snapshot CreateSnapshot(SerializedProperty collection, Type collectionType)
        {
            return new Snapshot
            {
                SourceObject = collection.serializedObject.targetObject,
                PropertyPath = collection.propertyPath,
                CollectionType = collectionType
            };
        }

        private static bool PasteSnapshot(
            Snapshot snapshot,
            SerializedProperty destination,
            Type destinationType)
        {
            if (snapshot == null || snapshot.SourceObject == null || snapshot.CollectionType != destinationType)
            {
                return false;
            }

            var sourceObject = new SerializedObject(snapshot.SourceObject);
            SerializedProperty source = sourceObject.FindProperty(snapshot.PropertyPath);
            if (source == null)
            {
                return false;
            }

            destination.arraySize = source.arraySize;
            for (int index = 0; index < source.arraySize; index++)
            {
                SerializedPropertyCopyUtility.Copy(
                    source.GetArrayElementAtIndex(index),
                    destination.GetArrayElementAtIndex(index));
            }

            destination.serializedObject.ApplyModifiedProperties();
            return true;
        }
    }
}
