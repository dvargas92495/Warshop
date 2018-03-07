using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuItemController : MonoBehaviour {

    Action callback;
    internal Color inactiveColor = new Color(0.25f, 0.25f, 0.25f);
    internal Color onHover = Color.gray;
    internal Color offHover = Color.white;
    private bool inactive;

    void OnMouseEnter()
    {
        if (!inactive) GetComponent<SpriteRenderer>().color = onHover;
    }

    void OnMouseExit()
    {
        if (!inactive) GetComponent<SpriteRenderer>().color = offHover;
    }

    void OnMouseUp()
    {
        if (!inactive)
        {
            GetComponent<SpriteRenderer>().color = offHover;
            callback();
        }
    }

    public void SetCallback(Action c)
    {
        callback = c;
    }
    

    public void Deactivate()
    {
        inactive = true;
        GetComponent<SpriteRenderer>().color = inactiveColor;
    }

    public void Activate()
    {
        inactive = false;
        GetComponent<SpriteRenderer>().color = offHover;
    }

    public void SetActive(bool b)
    {
        if (b) Activate();
        else Deactivate();
    }
}
