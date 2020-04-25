using UnityEditor;
using UnityEngine;
 
public class BuildServer: MonoBehaviour
{
      static void Start()
     {
         string[] scenes = {"Assets/Scenes/Server.unity" };
         BuildPipeline.BuildPlayer(scenes, "ServerBuild/App.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
     }
}