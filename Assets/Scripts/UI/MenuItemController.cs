using System;
using UnityEngine;

public class MenuItemController : MonoBehaviour {

    Action callback;
    public MeshRenderer background;
    public Material onHover;
    public Material offHover;
    internal bool isSubMenu;
    internal bool clicked;

    void OnMouseEnter()
    {
        bool otherClicked = false;
        Array.ForEach(transform.parent.GetComponentsInChildren<MenuItemController>(), (MenuItemController mi) => otherClicked = otherClicked || mi.clicked);
        if (!otherClicked || isSubMenu)
        {
            background.material = onHover;
        }
    }

    void OnMouseExit()
    {
        if (!clicked)
        {
            background.material = offHover;
        }
    }

    void OnMouseUp()
    {
        bool shouldClick = !clicked;
        foreach(MenuItemController mi in transform.parent.GetComponentsInChildren<MenuItemController>())
        {
            if (mi.clicked)
            {
                mi.clicked = false;
                if (mi.Equals(this))
                {
                    Array.ForEach(transform.parent.GetComponentsInChildren<MenuItemController>(), (MenuItemController m) =>
                    {
                        if (m.isSubMenu) Destroy(m.gameObject);
                    });
                } else
                {
                    mi.background.material = offHover;
                }
            }
        }
        if (shouldClick)
        {
            clicked = true;
            background.material = onHover;
            callback();
        }
    }

    public void SetCallback(Action c)
    {
        callback = c;
    }
}
