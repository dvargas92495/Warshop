using System;
using UnityEngine;

public class MenuItemController : MonoBehaviour {

    Action callback;
    public MeshRenderer background;
    public Material onHover;
    public Material offHover;

    void OnMouseEnter()
    {
        background.material = onHover;
    }

    void OnMouseExit()
    {
        background.material = offHover;
    }

    void OnMouseUp()
    {
        callback();
    }

    public void SetCallback(Action c)
    {
        callback = c;
    }
}
