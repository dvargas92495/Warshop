using NUnit.Framework;
using System.IO;

public class ScriptConventionsTests
{
    private static Util.List<string> allFiles;

    [OneTimeSetUp]
    public void Setup()
    {
        allFiles = GetAllFiles("Assets/Scripts");
    }

    [Test]
    public void NoGetComponentCalls()
    {
        Util.List<string> filesWithGetComponent = allFiles.Filter(NoGetComponentHelper);
        Assert.IsTrue(filesWithGetComponent.IsEmpty(), "Invalid Files with GetComponent Calls: \n" + filesWithGetComponent.ToString(",\n"));
    }

    private static bool NoGetComponentHelper(string file)
    {
        string content = File.ReadAllText(file);
        return content.Contains("GetComponent");
    }

    private static Util.List<string> GetAllFiles(string dirPath)
    {
        string[] allDirectories = Directory.GetDirectories(dirPath);
        string[] files = Directory.GetFiles(dirPath, "*.cs");
        Util.List<string> moreFiles = Util.ToList(allDirectories).MapFlattened(GetAllFiles);
        return Util.ToList(files).Concat(moreFiles);
    }
}
