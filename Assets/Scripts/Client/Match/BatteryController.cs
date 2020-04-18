using System;
using UnityEngine;
using UnityEngine.Events;

public class BatteryController : Controller
{
    public MeshRenderer scoreMeshRenderer;
    public Renderer coreRenderer;
    public TextMesh score;
    public AnimatorHelper animatorHelper;

    void Start()
    {
        scoreMeshRenderer.sortingOrder = 2;
    }

    internal void DisplayDamage(UnityAction callback)
    {
        animatorHelper.Animate("BatteryDamage", callback);
    }

    internal void DisplayEnd(UnityAction callback)
    {
        animatorHelper.Animate("EndGame", callback);
    }
}
