using System;
using UnityEngine;

public class MenuItemController : MonoBehaviour {

    Action callback;
    internal Color onHover = Color.gray;
    internal Color offHover = Color.white;

    void OnMouseEnter()
    {
        GetComponent<SpriteRenderer>().color = onHover;
    }

    void OnMouseExit()
    {
        GetComponent<SpriteRenderer>().color = offHover;
    }

    void OnMouseUp()
    {
        GetComponent<SpriteRenderer>().color = offHover;
        callback();
    }

    public void SetCallback(Action c)
    {
        callback = c;
    }
}
