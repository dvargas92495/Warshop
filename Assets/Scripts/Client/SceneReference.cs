using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// https://gist.github.com/JohannesMP/ec7d3f0bcf167dab3d0d3bb480e0e07b
[System.Serializable]
public class SceneReference : ISerializationCallbackReceiver
{
    [SerializeField] private Object sceneAsset = null;
    [SerializeField] private string scenePath = string.Empty;

#if UNITY_EDITOR
    bool IsValidSceneAsset
    {
        get
        {
            if (sceneAsset == null) return false;
            return sceneAsset.GetType().Equals(typeof(SceneAsset));
        }
    }
#endif

    public string ScenePath
    {
        get
        {
#if UNITY_EDITOR
            return GetScenePathFromAsset();
#else
            return scenePath;
#endif
        }
        set
        {
            scenePath = value;
#if UNITY_EDITOR
            sceneAsset = GetSceneAssetFromPath();
#endif
        }
    }

    public static implicit operator string(SceneReference sceneReference)
    {
        return sceneReference.ScenePath;
    }

    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        HandleBeforeSerialize();
#endif
    }

    public void OnAfterDeserialize()
    {
#if UNITY_EDITOR
        EditorApplication.update += HandleAfterDeserialize;
#endif
    }

#if UNITY_EDITOR
    private SceneAsset GetSceneAssetFromPath()
    {
        if (string.IsNullOrEmpty(scenePath))
            return null;
        return AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
    }

    private string GetScenePathFromAsset()
    {
        if (sceneAsset == null)
            return string.Empty;
        return AssetDatabase.GetAssetPath(sceneAsset);
    }

    private void HandleBeforeSerialize()
    {
        if (!IsValidSceneAsset && !string.IsNullOrEmpty(scenePath))
        {
            sceneAsset = GetSceneAssetFromPath();
            if (sceneAsset == null)
                scenePath = string.Empty;

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }
        else
        {
            scenePath = GetScenePathFromAsset();
        }
    }

    private void HandleAfterDeserialize()
    {
        EditorApplication.update -= HandleAfterDeserialize;
        if (IsValidSceneAsset) return;

        if (string.IsNullOrEmpty(scenePath) == false)
        {
            sceneAsset = GetSceneAssetFromPath();
            if (sceneAsset == null) scenePath = string.Empty;

            if (!Application.isPlaying) UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }
    }
#endif
}
