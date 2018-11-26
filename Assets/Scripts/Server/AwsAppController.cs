using UnityEngine;

public class AwsAppController : MonoBehaviour 
{
    private void Awake()
    {
        App.StartServer();
    }
}
