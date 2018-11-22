using UnityEngine;

public class BatteryController : Controller
{
    public TextMesh Score;

    void Start()
    {
        Score.GetComponent<MeshRenderer>().sortingOrder = 1;
    }
}
