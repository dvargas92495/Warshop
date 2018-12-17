using UnityEngine;

public class BatteryController : Controller
{
    public MeshRenderer scoreMeshRenderer;
    public Renderer coreRenderer;
    public TextMesh score;

    void Start()
    {
        scoreMeshRenderer.sortingOrder = 2;
    }
}
