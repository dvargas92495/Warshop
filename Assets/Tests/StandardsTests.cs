using UnityEngine;
using NUnit.Framework;
using System.IO;

public class StandardsTests : MonoBehaviour 
{
    private static string[] allFiles;

    [OneTimeSetUp]
    public void Setup()
    {
        allFiles = GetAllFiles("Assets/Scripts");
    }

    [Test]
    public void NoGetComponentCalls()
    {
        string[] filesWithGetComponent = Util.Filter(allFiles, NoGetComponentHelper);
        Assert.AreEqual(0, filesWithGetComponent.Length, "Invalid Files with GetComponent Calls: \n" + Util.ToArrayString(filesWithGetComponent, ",\n"));
    }

    private static bool NoGetComponentHelper(string file)
    {
        string content = File.ReadAllText(file);
        return content.Contains("GetComponent");
    }

    private static string[] GetAllFiles(string dirPath)
    {
        string[] allDirectories = Directory.GetDirectories(dirPath);
        string[] files = Directory.GetFiles(dirPath, "*.cs");
        string[] moreFiles = Util.Flatten(Util.Map(allDirectories, GetAllFiles));
        return Util.Concat(files, moreFiles);
    }
}
