using UnityEngine;

public class AwsAppController : MonoBehaviour 
{
    public void Awake()
    {
        App.StartServer();
    }
}
