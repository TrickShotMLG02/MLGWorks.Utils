using UnityEditor;
using UnityEngine;
using System.IO;
using MLGWorks.Utils.Logging;
using Logger = MLGWorks.Utils.Logging.Logger;

[CustomEditor(typeof(Logger))]
public class LoggerEditor : Editor
{
    // Serialized props for every public field on Logger
    private SerializedProperty pathTypeProp;
    private SerializedProperty relativePathProp;
    private SerializedProperty customPathProp;

    private SerializedProperty debugEnabledProp;
    private SerializedProperty maxLogFileCountProp;
    private SerializedProperty fileExtensionProp;

    private SerializedProperty debugTargetsProp;
    private SerializedProperty infoTargetsProp;
    private SerializedProperty warningTargetsProp;
    private SerializedProperty errorTargetsProp;

    private static readonly string[] fileOptions = {
        "Debug File", "Info File", "Warning File", "Error File", "Combined File"
    };

    private void OnEnable()
    {
        pathTypeProp = serializedObject.FindProperty("pathType");
        relativePathProp = serializedObject.FindProperty("relativePath");
        customPathProp = serializedObject.FindProperty("customPath");

        debugEnabledProp = serializedObject.FindProperty("enableDebugLogging");
        maxLogFileCountProp = serializedObject.FindProperty("maxLogFileCount");
        fileExtensionProp = serializedObject.FindProperty("fileExtension");

        debugTargetsProp = serializedObject.FindProperty("debugTargets");
        infoTargetsProp = serializedObject.FindProperty("infoTargets");
        warningTargetsProp = serializedObject.FindProperty("warningTargets");
        errorTargetsProp = serializedObject.FindProperty("errorTargets");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Logger Configuration", EditorStyles.boldLabel);

        // Path settings
        EditorGUILayout.PropertyField(pathTypeProp, new GUIContent("Log Location"));
        if ((LogLocationType)pathTypeProp.enumValueIndex != LogLocationType.Custom)
        {
            EditorGUILayout.PropertyField(relativePathProp, new GUIContent("Relative Path"));
        }
        else
        {
            EditorGUILayout.PropertyField(customPathProp, new GUIContent("Custom Full Path"));
        }

        EditorGUILayout.Space();

        // Logging options
        EditorGUILayout.PropertyField(debugEnabledProp, new GUIContent("Enable Debug Logging"));
        EditorGUILayout.PropertyField(maxLogFileCountProp, new GUIContent("Max Log File Count"));
        EditorGUILayout.HelpBox("-1 = keep all files", MessageType.Info);
        EditorGUILayout.PropertyField(fileExtensionProp, new GUIContent("Log File Extension"));

        EditorGUILayout.Space();

        // Targets masks
        EditorGUILayout.LabelField("Log Level → File Targets", EditorStyles.boldLabel);
        DrawMask("Debug Targets", debugTargetsProp);
        DrawMask("Info Targets", infoTargetsProp);
        DrawMask("Warning Targets", warningTargetsProp);
        DrawMask("Error Targets", errorTargetsProp);

        EditorGUILayout.Space();

        // Open folder button
        if (GUILayout.Button("Open Log Folder"))
        {
            var logger = (Logger)target;
            string path = logger.LogDirectory;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawMask(string label, SerializedProperty maskProp)
    {
        maskProp.intValue = EditorGUILayout.MaskField(label, maskProp.intValue, fileOptions);
    }
}
