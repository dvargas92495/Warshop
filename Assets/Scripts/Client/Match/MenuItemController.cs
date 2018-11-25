using UnityEngine;
using UnityEngine.Events;

public class MenuItemController : Controller
{
    public Material inactiveRing;
    public Material activeRing;
    public Material inactiveBase;
    public Material activeBase;
    public MeshRenderer baseRenderer;
    public MeshRenderer ringRenderer;
    public SpriteRenderer spriteRenderer;

    private bool inactive;
    private bool selected;
    private UnityAction callback;


    void OnMouseUp()
    {
        Click();
    }

    public void SetCallback(UnityAction c)
    {
        callback = c;
    }

    public void SetSprite(Sprite s)
    {
        spriteRenderer.sprite = s;
    }

    public void ClearSprite()
    {
        spriteRenderer.sprite = null;
    }

    public void Click()
    {
        if (!inactive && !selected)
        {
            Select();
            callback();
        }
    }
    

    public void Deactivate()
    {
        inactive = true;
        selected = false;
        baseRenderer.material = inactiveBase;
        ringRenderer.material = inactiveRing;
        baseRenderer.transform.localPosition = Vector3.up*0.225f;
    }

    public void Select()
    {
        selected = true;
        baseRenderer.material = inactiveBase;
        baseRenderer.transform.localPosition = Vector3.zero;
    }

    public void Activate()
    {
        inactive = false;
        selected = false;
        baseRenderer.material = activeBase;
        ringRenderer.material = activeRing;
        baseRenderer.transform.localPosition = Vector3.up * 0.225f;
    }

    public void SetActive(bool b)
    {
        if (b) Activate();
        else Deactivate();
    }

    public bool IsSelected()
    {
        return selected;
    }
}
