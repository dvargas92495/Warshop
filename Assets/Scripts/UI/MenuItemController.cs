using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuItemController : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler {

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

    public void OnPointerClick(PointerEventData eventData)
    {
        GetComponent<Image>().color = offHover;
        callback();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        GetComponent<Image>().color = onHover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetComponent<Image>().color = offHover;
    }
}
