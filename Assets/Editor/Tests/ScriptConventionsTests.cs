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

    [Test]
    public void NoSystemDependencies()
    {
        Util.List<string> filesWithSystemDependence = allFiles.Filter(NoSystemDependencyHelper).Filter(f => !f.EndsWith("ZException.cs"));
        Assert.IsTrue(filesWithSystemDependence.IsEmpty(), "Invalid Files with System dependency: \n" + filesWithSystemDependence.ToString(",\n"));
    }

    private static bool NoGetComponentHelper(string file)
    {
        return FileContainsHelper(file, "GetComponent");
    }

    private static bool NoSystemDependencyHelper(string file)
    {
        return FileContainsHelper(file, "using System");
    }

    private static bool FileContainsHelper(string file, string phrase)
    {
        string content = File.ReadAllText(file);
        return content.Contains(phrase);
    }

    private static Util.List<string> GetAllFiles(string dirPath)
    {
        string[] allDirectories = Directory.GetDirectories(dirPath);
        string[] files = Directory.GetFiles(dirPath, "*.cs");
        Util.List<string> moreFiles = Util.ToList(allDirectories).MapFlattened(GetAllFiles);
        return Util.ToList(files).Concat(moreFiles);
    }
}
