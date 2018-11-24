using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;

public class EditorValidator : EditorWindow
{
    [MenuItem("Component/Validate All References")]
    public static void ValidateAllScenes()
    {
        // https://stackoverflow.com/questions/40577412/clear-editor-console-logs-from-script
        // Clear Log
        Assembly editor = Assembly.GetAssembly(typeof(Editor));
        editor.GetType("UnityEditor.LogEntries").GetMethod("Clear").Invoke(new object(), null);

        string[] sceneNames = Util.Map(EditorBuildSettings.scenes, s => s.path);
        string[] errors = Util.Flatten(Util.Map(sceneNames, i => ValidateScene(i)));
        Util.ForEach(errors, Debug.LogError);
        if (errors.Length == 0) Debug.Log("No Errors");
    }

    private static string[] ValidateScene(string s)
    {
        EditorSceneManager.OpenScene(s);
        Controller[] controllers = FindObjectsOfType<Controller>();
        return Util.Flatten(Util.Map(controllers, ValidateController));
    }

    private static string[] ValidateController(Controller c)
    {
        FieldInfo[] nullFields = Util.Filter(c.GetType().GetFields(), f => f.GetValue(c) == null);
        return Util.Map(nullFields, f => "Missing reference on " + c.name + ":" + c.GetType().Name + " for field " + f.Name);
    }
}
