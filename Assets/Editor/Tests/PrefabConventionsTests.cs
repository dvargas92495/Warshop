using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.Reflection;

public class PrefabConventionsTests
{
    [Test]
    public void AllControllerReferencesAreNotNull()
    {
        List<string> sceneNames = Util.ToList(EditorBuildSettings.scenes).Map(s => s.path);
        List<string> prefabPaths = Util.ToList(AssetDatabase.GetAllAssetPaths()).Filter(s => s.StartsWith("Assets/Prefabs/") && s.EndsWith(".prefab"));
        List<string> errors = sceneNames.MapFlattened(ValidateScene).Concat(prefabPaths.MapFlattened(ValidatePrefab));
        Assert.IsTrue(errors.IsEmpty(), "Missing references on :\n" + errors.ToString(",\n"));
    }

    private static List<string> ValidateScene(string s)
    {
        EditorSceneManager.OpenScene(s);
        Controller[] controllers = Object.FindObjectsOfType<Controller>();
        return Util.ToList(controllers).MapFlattened(ValidateController);
    }

    private static List<string> ValidateController(Controller c)
    {
        List<FieldInfo> nullFields = Util.ToList(c.GetType().GetFields()).Filter(f => f.GetValue(c) == null);
        return nullFields.Map(f => c.name + " - " + c.GetType().Name + " - " + f.Name);
    }

    private static List<string> ValidatePrefab(string p)
    {
        Object g = AssetDatabase.LoadMainAssetAtPath(p);
        Controller c = ((GameObject)g).GetComponent<Controller>();
        if (c != null) return ValidateController(c);
        return Util.ToList<string>();
    }
}
