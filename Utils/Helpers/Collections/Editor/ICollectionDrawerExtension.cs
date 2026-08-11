using System;
using UnityEditor;
using UnityEngine;

namespace MLGWorks.Utils.Helpers.Collections.Editor
{
    /// <summary>Provides a custom inspector implementation for a serializable collection type.</summary>
    public interface ICollectionDrawerExtension
    {
        /// <summary>Draws the collection and returns the height used by the drawer.</summary>
        /// <param name="position">The available inspector rectangle.</param>
        /// <param name="property">The serialized collection property.</param>
        /// <param name="label">The property label.</param>
        void OnGUI(Rect position, SerializedProperty property, GUIContent label);

        /// <summary>Gets the height needed to draw the collection.</summary>
        /// <param name="property">The serialized collection property.</param>
        /// <param name="label">The property label.</param>
        /// <returns>The required height in pixels.</returns>
        float GetPropertyHeight(SerializedProperty property, GUIContent label);

        /// <summary>Gets whether this drawer handles the requested collection type.</summary>
        /// <param name="collectionType">The reflected field type.</param>
        /// <returns>True when this extension should draw the property.</returns>
        bool CanDraw(Type collectionType);
    }
}
