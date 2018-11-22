using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Reflection;

public class EditorValidator : EditorWindow
{
    [MenuItem("GameObject/Validate")]
    public static void ValidateAllScenes()
    {
        // https://stackoverflow.com/questions/40577412/clear-editor-console-logs-from-script
        // Clear Log
        Assembly editor = Assembly.GetAssembly(typeof(Editor));
        editor.GetType("UnityEditor.LogEntries").GetMethod("Clear").Invoke(new object(), null);

        string[] sceneNames = new string[] { "Initial", "Lobby", "Profile", "Setup", "Match" };
        string[] errors = Util.Flatten(Util.Map(sceneNames, i => ValidateScene(i)));
        Util.ForEach(errors, Debug.LogError);
    }

    private static string[] ValidateScene(string s)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/"+s+".unity");
        Controller[] controllers = FindObjectsOfType<Controller>();
        return Util.Flatten(Util.Map(controllers, ValidateController));
    }

    private static string[] ValidateController(Controller c)
    {
        FieldInfo[] nullFields = Util.Filter(c.GetType().GetFields(), f => f.GetValue(c) == null);
        return Util.Map(nullFields, f => "Missing reference on " + c.name + ":" + c.GetType().Name + " for field " + f.Name);
    }
}
