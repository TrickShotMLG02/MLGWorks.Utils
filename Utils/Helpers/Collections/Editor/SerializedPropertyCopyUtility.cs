using UnityEditor;
using UnityEngine;

namespace MLGWorks.Utils.Helpers.Collections.Editor
{
    /// <summary>Copies supported serialized property values, including nested generic entries.</summary>
    internal static class SerializedPropertyCopyUtility
    {
        /// <summary>Copies the source value into the destination property.</summary>
        /// <param name="source">The source property.</param>
        /// <param name="destination">The destination property.</param>
        public static void Copy(SerializedProperty source, SerializedProperty destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            switch (source.propertyType)
            {
                case SerializedPropertyType.Integer:
                    destination.longValue = source.longValue;
                    break;
                case SerializedPropertyType.Boolean:
                    destination.boolValue = source.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    destination.doubleValue = source.doubleValue;
                    break;
                case SerializedPropertyType.String:
                    destination.stringValue = source.stringValue;
                    break;
                case SerializedPropertyType.Color:
                    destination.colorValue = source.colorValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    destination.objectReferenceValue = source.objectReferenceValue;
                    break;
                case SerializedPropertyType.LayerMask:
                    destination.intValue = source.intValue;
                    break;
                case SerializedPropertyType.Enum:
                    destination.enumValueIndex = source.enumValueIndex;
                    break;
                case SerializedPropertyType.Vector2:
                    destination.vector2Value = source.vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    destination.vector3Value = source.vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    destination.vector4Value = source.vector4Value;
                    break;
                case SerializedPropertyType.Rect:
                    destination.rectValue = source.rectValue;
                    break;
                case SerializedPropertyType.ArraySize:
                    destination.intValue = source.intValue;
                    break;
                case SerializedPropertyType.Character:
                    destination.intValue = source.intValue;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    destination.animationCurveValue = source.animationCurveValue;
                    break;
                case SerializedPropertyType.Bounds:
                    destination.boundsValue = source.boundsValue;
                    break;
                case SerializedPropertyType.Quaternion:
                    destination.quaternionValue = source.quaternionValue;
                    break;
                case SerializedPropertyType.Vector2Int:
                    destination.vector2IntValue = source.vector2IntValue;
                    break;
                case SerializedPropertyType.Vector3Int:
                    destination.vector3IntValue = source.vector3IntValue;
                    break;
                case SerializedPropertyType.RectInt:
                    destination.rectIntValue = source.rectIntValue;
                    break;
                case SerializedPropertyType.BoundsInt:
                    destination.boundsIntValue = source.boundsIntValue;
                    break;
                default:
                    CopyChildren(source, destination);
                    break;
            }
        }

        private static void CopyChildren(SerializedProperty source, SerializedProperty destination)
        {
            SerializedProperty sourceIterator = source.Copy();
            SerializedProperty sourceEnd = source.GetEndProperty();
            bool enterChildren = true;
            while (sourceIterator.NextVisible(enterChildren) &&
                   !SerializedProperty.EqualContents(sourceIterator, sourceEnd))
            {
                enterChildren = false;
                if (sourceIterator.depth != source.depth + 1)
                {
                    continue;
                }

                SerializedProperty destinationChild = destination.FindPropertyRelative(sourceIterator.name);
                Copy(sourceIterator, destinationChild);
            }
        }
    }
}
