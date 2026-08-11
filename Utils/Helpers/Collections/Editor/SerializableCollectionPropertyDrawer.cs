using System;
using System.Collections.Generic;
using MLGWorks.Utils.Helpers.Collections;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MLGWorks.Utils.Helpers.Collections.Editor
{
    /// <summary>
    /// Draws serializable collection fields with reordering, filtering, validation, and cleanup controls.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializableCollectionBase), true)]
    public sealed class SerializableCollectionPropertyDrawer : PropertyDrawer
    {
        private const float Spacing = 2f;
        private const float ToolbarHeight = 20f;
        private const float ReorderHandleWidth = 18f;
        private const float ListRightMargin = 4f;
        private static readonly Dictionary<string, ReorderableList> lists = new();
        private static readonly Dictionary<string, string> searchTerms = new();

        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ICollectionDrawerExtension extension = CollectionDrawerRegistry.Find(GetCollectionType());
            if (extension != null)
            {
                return extension.GetPropertyHeight(property, label);
            }

            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight + Spacing;
            SerializedProperty policy = property.FindPropertyRelative("duplicateKeyPolicy");
            if (policy != null)
            {
                height += EditorGUIUtility.singleLineHeight + Spacing;
            }

            SerializedProperty collection = FindCollectionList(property);
            if (collection == null)
            {
                return height;
            }

            ReorderableList list = GetList(property, collection);
            return height + list.GetHeight() + ToolbarHeight * 4f + Spacing * 4f;
        }

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            ICollectionDrawerExtension extension = CollectionDrawerRegistry.Find(GetCollectionType());
            if (extension != null)
            {
                extension.OnGUI(position, property, label);
                EditorGUI.EndProperty();
                return;
            }

            SerializedProperty collection = FindCollectionList(property);
            Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(
                line,
                property.isExpanded,
                GetCountLabel(label, collection),
                true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            line.y += EditorGUIUtility.singleLineHeight + Spacing;

            SerializedProperty policy = property.FindPropertyRelative("duplicateKeyPolicy");
            if (policy != null)
            {
                EditorGUI.PropertyField(line, policy);
                line.y += EditorGUIUtility.singleLineHeight + Spacing;
            }

            if (collection != null)
            {
                ReorderableList list = GetList(property, collection);
                Rect listRect = new Rect(line.x, line.y, line.width, list.GetHeight());
                list.DoList(listRect);
                line.y += listRect.height + Spacing;

                DrawToolbar(new Rect(line.x, line.y, line.width, ToolbarHeight * 4f + Spacing * 3f), property, collection);
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private ReorderableList GetList(SerializedProperty property, SerializedProperty collection)
        {
            string key = GetCacheKey(property);
            if (lists.TryGetValue(key, out ReorderableList existing) && existing.serializedProperty != null)
            {
                return existing;
            }

            var list = new ReorderableList(property.serializedObject, collection, true, true, true, true)
            {
                draggable = true,
                displayAdd = true,
                displayRemove = true,
                headerHeight = EditorGUIUtility.singleLineHeight + Spacing,
                footerHeight = EditorGUIUtility.singleLineHeight + Spacing
            };

            list.drawHeaderCallback = rect =>
            {
                string currentSearch = GetSearchTerm(key);
                Rect labelRect = new Rect(rect.x, rect.y, Mathf.Max(70f, rect.width * 0.45f), rect.height);
                EditorGUI.LabelField(labelRect, $"Entries ({collection.arraySize})");
                Rect searchRect = new Rect(rect.x + labelRect.width, rect.y, rect.width - labelRect.width, rect.height);
                string nextSearch = EditorGUI.TextField(searchRect, currentSearch, GUI.skin.FindStyle("ToolbarSearchTextField"));
                if (nextSearch != currentSearch)
                {
                    searchTerms[key] = nextSearch;
                }
            };
            list.drawElementCallback = (rect, index, _, _) =>
            {
                if (!MatchesSearch(collection, index, GetSearchTerm(key)))
                {
                    return;
                }

                SerializedProperty element = collection.GetArrayElementAtIndex(index);
                string issue = GetValidationIssue(collection, index);
                if (!string.IsNullOrEmpty(issue))
                {
                    EditorGUI.DrawRect(
                        new Rect(
                            rect.x - ReorderHandleWidth,
                            rect.y,
                            rect.width + ReorderHandleWidth + ListRightMargin,
                            rect.height),
                        new Color(1f, 0.35f, 0.2f, 0.12f));
                }

                float propertyHeight = EditorGUI.GetPropertyHeight(element, true);
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, rect.width, propertyHeight),
                    element,
                    new GUIContent($"Element {index}"),
                    true);

                if (!string.IsNullOrEmpty(issue))
                {
                    EditorGUI.HelpBox(
                        new Rect(rect.x, rect.y + propertyHeight + Spacing, rect.width, 30f),
                        issue,
                        GetValidationMessageType(collection, index));
                }
            };
            list.elementHeightCallback = index =>
            {
                if (!MatchesSearch(collection, index, GetSearchTerm(key)))
                {
                    return 0f;
                }

                float height = EditorGUI.GetPropertyHeight(collection.GetArrayElementAtIndex(index), true);
                return string.IsNullOrEmpty(GetValidationIssue(collection, index))
                    ? height + Spacing
                    : height + 30f + Spacing * 2f;
            };
            list.onAddCallback = target =>
            {
                target.serializedProperty.arraySize++;
                target.serializedProperty.serializedObject.ApplyModifiedProperties();
            };
            list.onRemoveCallback = target =>
            {
                if (target.index >= 0 && target.index < target.serializedProperty.arraySize)
                {
                    target.serializedProperty.DeleteArrayElementAtIndex(target.index);
                    target.serializedProperty.serializedObject.ApplyModifiedProperties();
                }
            };

            lists[key] = list;
            return list;
        }

        private void DrawToolbar(Rect position, SerializedProperty property, SerializedProperty collection)
        {
            float buttonWidth = (position.width - Spacing * 2f) / 3f;
            Rect firstRow = new Rect(position.x, position.y, position.width, ToolbarHeight);
            Rect secondRow = new Rect(position.x, firstRow.yMax + Spacing, position.width, ToolbarHeight);
            Rect thirdRow = new Rect(position.x, secondRow.yMax + Spacing, position.width, ToolbarHeight);
            Rect fourthRow = new Rect(position.x, thirdRow.yMax + Spacing, position.width, ToolbarHeight);

            Rect keepFirstRect = new Rect(firstRow.x, firstRow.y, buttonWidth, firstRow.height);
            Rect keepLastRect = new Rect(keepFirstRect.xMax + Spacing, firstRow.y, buttonWidth, firstRow.height);
            Rect removeDuplicatesRect = new Rect(keepLastRect.xMax + Spacing, firstRow.y, buttonWidth, firstRow.height);

            Rect copyRect = new Rect(secondRow.x, secondRow.y, buttonWidth, secondRow.height);
            Rect pasteRect = new Rect(copyRect.xMax + Spacing, secondRow.y, buttonWidth, secondRow.height);
            Rect duplicateRect = new Rect(pasteRect.xMax + Spacing, secondRow.y, buttonWidth, secondRow.height);

            Rect savePresetRect = new Rect(thirdRow.x, thirdRow.y, buttonWidth, thirdRow.height);
            Rect loadPresetRect = new Rect(savePresetRect.xMax + Spacing, thirdRow.y, buttonWidth, thirdRow.height);
            Rect resetRect = new Rect(loadPresetRect.xMax + Spacing, thirdRow.y, buttonWidth, thirdRow.height);

            Rect removeInvalidRect = new Rect(fourthRow.x, fourthRow.y, buttonWidth, fourthRow.height);
            Rect sortRect = new Rect(removeInvalidRect.xMax + Spacing, fourthRow.y, buttonWidth, fourthRow.height);
            Rect clearRect = new Rect(sortRect.xMax + Spacing, fourthRow.y, buttonWidth, fourthRow.height);

            if (GUI.Button(keepFirstRect, "Keep First"))
            {
                ResolveDuplicates(property, collection, keepLast: false);
            }

            if (GUI.Button(keepLastRect, "Keep Last"))
            {
                ResolveDuplicates(property, collection, keepLast: true);
            }

            if (GUI.Button(removeDuplicatesRect, "Remove All Duplicates"))
            {
                ResolveDuplicates(property, collection, keepLast: false);
            }

            if (GUI.Button(copyRect, "Copy Entries"))
            {
                CollectionEditorClipboard.Copy(collection, GetCollectionType());
            }

            if (GUI.Button(pasteRect, "Paste Entries"))
            {
                CollectionEditorClipboard.Paste(collection, GetCollectionType());
            }

            if (GUI.Button(duplicateRect, "Duplicate Data"))
            {
                CollectionEditorClipboard.Duplicate(collection);
                property.serializedObject.ApplyModifiedProperties();
            }

            if (GUI.Button(savePresetRect, "Save Preset"))
            {
                CollectionEditorClipboard.SavePreset(collection, GetCollectionType());
            }

            if (GUI.Button(loadPresetRect, "Load Preset"))
            {
                CollectionEditorClipboard.LoadPreset(collection, GetCollectionType());
            }

            if (GUI.Button(resetRect, "Reset Defaults"))
            {
                collection.arraySize = 0;
                property.serializedObject.ApplyModifiedProperties();
            }

            if (GUI.Button(removeInvalidRect, "Remove Invalid"))
            {
                RemoveInvalidEntries(property, collection);
            }

            if (GUI.Button(sortRect, "Sort Entries"))
            {
                SortEntries(property, collection);
            }

            if (GUI.Button(clearRect, "Clear Collection"))
            {
                collection.arraySize = 0;
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        private void ResolveDuplicates(SerializedProperty property, SerializedProperty collection, bool keepLast)
        {
            if (IsLookupType())
            {
                return;
            }

            if (keepLast)
            {
                for (int index = 0; index < collection.arraySize; index++)
                {
                    if (HasDuplicateAfter(collection, index))
                    {
                        collection.DeleteArrayElementAtIndex(index--);
                    }
                }
            }
            else
            {
                for (int index = collection.arraySize - 1; index >= 0; index--)
                {
                    if (HasDuplicateBefore(collection, index))
                    {
                        collection.DeleteArrayElementAtIndex(index);
                    }
                }
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        private bool HasDuplicateBefore(SerializedProperty collection, int index)
        {
            SerializedProperty element = collection.GetArrayElementAtIndex(index);
            SerializedProperty key = element.FindPropertyRelative("Key") ?? element;
            for (int previous = 0; previous < index; previous++)
            {
                SerializedProperty previousElement = collection.GetArrayElementAtIndex(previous);
                SerializedProperty previousKey = element.FindPropertyRelative("Key") == null
                    ? previousElement
                    : previousElement.FindPropertyRelative("Key");
                if (previousKey != null && SerializedProperty.DataEquals(key, previousKey))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasDuplicateAfter(SerializedProperty collection, int index)
        {
            SerializedProperty element = collection.GetArrayElementAtIndex(index);
            SerializedProperty key = element.FindPropertyRelative("Key") ?? element;
            for (int next = index + 1; next < collection.arraySize; next++)
            {
                SerializedProperty nextElement = collection.GetArrayElementAtIndex(next);
                SerializedProperty nextKey = element.FindPropertyRelative("Key") == null
                    ? nextElement
                    : nextElement.FindPropertyRelative("Key");
                if (nextKey != null && SerializedProperty.DataEquals(key, nextKey))
                {
                    return true;
                }
            }

            return false;
        }

        private void RemoveInvalidEntries(SerializedProperty property, SerializedProperty collection)
        {
            for (int index = collection.arraySize - 1; index >= 0; index--)
            {
                if (!string.IsNullOrEmpty(GetValidationIssue(collection, index)) &&
                    GetValidationMessageType(collection, index) != MessageType.Info)
                {
                    collection.DeleteArrayElementAtIndex(index);
                }
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        private void SortEntries(SerializedProperty property, SerializedProperty collection)
        {
            for (int targetIndex = 0; targetIndex < collection.arraySize; targetIndex++)
            {
                int bestIndex = targetIndex;
                string bestText = GetSortText(collection.GetArrayElementAtIndex(bestIndex));
                for (int candidateIndex = targetIndex + 1; candidateIndex < collection.arraySize; candidateIndex++)
                {
                    string candidateText = GetSortText(collection.GetArrayElementAtIndex(candidateIndex));
                    if (string.CompareOrdinal(candidateText, bestText) < 0)
                    {
                        bestIndex = candidateIndex;
                        bestText = candidateText;
                    }
                }

                if (bestIndex != targetIndex)
                {
                    collection.MoveArrayElement(bestIndex, targetIndex);
                }
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        private bool MatchesSearch(SerializedProperty collection, int index, string search)
        {
            return string.IsNullOrWhiteSpace(search) ||
                   GetSortText(collection.GetArrayElementAtIndex(index))
                       .IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string GetValidationIssue(SerializedProperty collection, int index)
        {
            SerializedProperty element = collection.GetArrayElementAtIndex(index);
            if (element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue == null)
            {
                return "This serialized entry is null.";
            }

            SerializedProperty key = element.FindPropertyRelative("Key");
            if (key != null && key.propertyType == SerializedPropertyType.ObjectReference && key.objectReferenceValue == null)
            {
                return "Dictionary and lookup keys cannot be null.";
            }

            if (key == null || IsLookupType())
            {
                if (FindDuplicate(collection, index, key))
                {
                    return IsLookupType()
                        ? "This lookup key intentionally has multiple values."
                        : "This serialized value is duplicated.";
                }

                return string.Empty;
            }

            return FindDuplicate(collection, index, key)
                ? "This serialized key is duplicated."
                : string.Empty;
        }

        private bool FindDuplicate(SerializedProperty collection, int index, SerializedProperty key)
        {
            SerializedProperty current = key ?? collection.GetArrayElementAtIndex(index);
            for (int previous = 0; previous < index; previous++)
            {
                SerializedProperty previousElement = collection.GetArrayElementAtIndex(previous);
                SerializedProperty previousKey = key == null
                    ? previousElement
                    : previousElement.FindPropertyRelative("Key");
                if (previousKey != null && SerializedProperty.DataEquals(current, previousKey))
                {
                    return true;
                }
            }

            return false;
        }

        private MessageType GetValidationMessageType(SerializedProperty collection, int index)
        {
            SerializedProperty element = collection.GetArrayElementAtIndex(index);
            SerializedProperty key = element.FindPropertyRelative("Key");
            if ((key != null && key.propertyType == SerializedPropertyType.ObjectReference && key.objectReferenceValue == null) ||
                (element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue == null))
            {
                return MessageType.Warning;
            }

            return IsLookupType() && FindDuplicate(
                collection,
                index,
                key)
                ? MessageType.Info
                : MessageType.Warning;
        }

        private static string GetSortText(SerializedProperty property)
        {
            SerializedProperty key = property.FindPropertyRelative("Key");
            return PropertyToText(key ?? property);
        }

        private static string PropertyToText(SerializedProperty property)
        {
            if (property == null)
            {
                return string.Empty;
            }

            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                    return property.stringValue ?? string.Empty;
                case SerializedPropertyType.Integer:
                    return property.longValue.ToString();
                case SerializedPropertyType.Float:
                    return property.doubleValue.ToString();
                case SerializedPropertyType.Boolean:
                    return property.boolValue.ToString();
                case SerializedPropertyType.Enum:
                    return property.enumDisplayNames.Length > property.enumValueIndex && property.enumValueIndex >= 0
                        ? property.enumDisplayNames[property.enumValueIndex]
                        : property.enumValueIndex.ToString();
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue == null ? string.Empty : property.objectReferenceValue.name;
                default:
                    return property.displayName;
            }
        }

        private string GetCacheKey(SerializedProperty property)
        {
            UnityEngine.Object target = property.serializedObject.targetObject;
            return $"{target.GetInstanceID()}:{property.propertyPath}";
        }

        private string GetSearchTerm(string key)
        {
            return searchTerms.TryGetValue(key, out string value) ? value : string.Empty;
        }

        private Type GetCollectionType() => fieldInfo?.FieldType;

        private bool IsLookupType()
        {
            Type type = GetCollectionType();
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SerializableLookup<,>))
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static SerializedProperty FindCollectionList(SerializedProperty property)
        {
            return property.FindPropertyRelative("entries") ?? property.FindPropertyRelative("items");
        }

        private static GUIContent GetCountLabel(GUIContent label, SerializedProperty collection)
        {
            if (collection == null)
            {
                return label;
            }

            return new GUIContent(
                $"{label.text} ({collection.arraySize})",
                label.image,
                label.tooltip);
        }
    }
}
