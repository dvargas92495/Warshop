using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuItemController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {

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

    public void OnPointerClick(PointerEventData eventData)
    {

        if (!inactive)
        {
            GetComponent<Image>().color = offHover;
            callback();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!inactive) GetComponent<Image>().color = onHover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!inactive) GetComponent<Image>().color = offHover;
    }

    public void Deactivate()
    {
        inactive = true;
        if (GetComponent<SpriteRenderer>() != null)
        {
            GetComponent<SpriteRenderer>().color = inactiveColor;
        } else
        {
            GetComponent<Image>().color = inactiveColor;
        }
    }

    public void Activate()
    {
        inactive = false;
        if (GetComponent<SpriteRenderer>() != null)
        {
            GetComponent<SpriteRenderer>().color = offHover;
        }
        else
        {
            GetComponent<Image>().color = offHover;
        }
    }
}
