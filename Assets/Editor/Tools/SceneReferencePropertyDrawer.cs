using UnityEngine;
using UnityEditor;

// https://gist.github.com/JohannesMP/ec7d3f0bcf167dab3d0d3bb480e0e07b
[CustomPropertyDrawer(typeof(SceneReference))]
public class SceneReferencePropertyDrawer : PropertyDrawer
{
    const string sceneAssetPropertyString = "sceneAsset";
    const string scenePathPropertyString = "scenePath";

    static readonly float padSize = 2f;
    static readonly float lineHeight = EditorGUIUtility.singleLineHeight;
    static readonly float paddedLine = lineHeight + padSize;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var sceneAssetProperty = GetSceneAssetProperty(property);

        position.height = lineHeight;

        EditorGUI.BeginProperty(position, GUIContent.none, property);
        EditorGUI.BeginChangeCheck();
        int sceneControlID = GUIUtility.GetControlID(FocusType.Passive);
        var selectedObject = EditorGUI.ObjectField(position, label, sceneAssetProperty.objectReferenceValue, typeof(SceneAsset), false);
        BuildScene buildScene = GetBuildScene(selectedObject);

        if (EditorGUI.EndChangeCheck())
        {
            sceneAssetProperty.objectReferenceValue = selectedObject;
            if (buildScene.scene == null) GetScenePathProperty(property).stringValue = string.Empty;
        }
        position.y += paddedLine;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return lineHeight;
    }

    static SerializedProperty GetSceneAssetProperty(SerializedProperty property)
    {
        return property.FindPropertyRelative(sceneAssetPropertyString);
    }

    static SerializedProperty GetScenePathProperty(SerializedProperty property)
    {
        return property.FindPropertyRelative(scenePathPropertyString);
    }

    public struct BuildScene
    {
        public int buildIndex;
        public GUID assetGUID;
        public string assetPath;
        public EditorBuildSettingsScene scene;
    }

    static public BuildScene GetBuildScene(Object sceneObject)
    {
        BuildScene entry = new BuildScene()
        {
            buildIndex = -1,
            assetGUID = new GUID(string.Empty)
        };

        if (sceneObject as SceneAsset == null) return entry;

        entry.assetPath = AssetDatabase.GetAssetPath(sceneObject);
        entry.assetGUID = new GUID(AssetDatabase.AssetPathToGUID(entry.assetPath));

        for (int index = 0; index < EditorBuildSettings.scenes.Length; ++index)
        {
            if (entry.assetGUID.Equals(EditorBuildSettings.scenes[index].guid))
            {
                entry.scene = EditorBuildSettings.scenes[index];
                entry.buildIndex = index;
                return entry;
            }
        }

        return entry;
    }
}
