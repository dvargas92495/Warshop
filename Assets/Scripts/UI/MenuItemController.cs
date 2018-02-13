using System;
using UnityEngine;

public class MenuItemController : MonoBehaviour {

    Action callback;
    public SpriteRenderer background;
    internal Color onHover = Color.gray;
    internal Color offHover = new Color(0.75f, 0.75f, 0.75f);
    internal bool isSubMenu;
    internal bool clicked;

    void OnMouseEnter()
    {
        bool otherClicked = false;
        Array.ForEach(transform.parent.GetComponentsInChildren<MenuItemController>(), (MenuItemController mi) => otherClicked = otherClicked || mi.clicked);
        if (!otherClicked || isSubMenu)
        {
            background.color = onHover;
        }
    }

    void OnMouseExit()
    {
        if (!clicked)
        {
            background.color = offHover;
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
                    mi.background.color = offHover;
                }
            }
        }
        if (shouldClick)
        {
            clicked = true;
            background.color = onHover;
            callback();
        }
    }

    public void SetCallback(Action c)
    {
        callback = c;
    }
}
