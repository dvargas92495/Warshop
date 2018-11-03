using UnityEngine;

public class BatteryController : MonoBehaviour
{
    public TextMesh Score;

    void Start()
    {
        Score.GetComponent<MeshRenderer>().sortingOrder = 1;
    }
}
