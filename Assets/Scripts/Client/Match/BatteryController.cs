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

    internal void DisplayDamage(short cost, UnityAction callback)
    {
        score.text = (int.Parse(score.text) - cost).ToString();
        animatorHelper.Animate("BatteryDamage", callback);
    }
}
