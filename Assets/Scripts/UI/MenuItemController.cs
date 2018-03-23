using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuItemController : MonoBehaviour {

    Action callback;
    public Material inactiveRing;
    public Material activeRing;
    public Material inactiveBase;
    public Material activeBase;
    private bool inactive;
    private bool selected;

    void OnMouseUp()
    {
        Click();
    }

    public void SetCallback(Action c)
    {
        callback = c;
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
        transform.GetChild(0).GetComponent<MeshRenderer>().material = inactiveBase;
        transform.GetChild(1).GetComponent<MeshRenderer>().material = inactiveRing;
        transform.GetChild(0).localPosition = Vector3.up*0.225f;
    }

    public void Select()
    {
        selected = true;
        transform.GetChild(0).GetComponent<MeshRenderer>().material = inactiveBase;
        transform.GetChild(0).localPosition = Vector3.zero;
    }

    public void Activate()
    {
        inactive = false;
        selected = false;
        transform.GetChild(0).GetComponent<MeshRenderer>().material = activeBase;
        transform.GetChild(1).GetComponent<MeshRenderer>().material = activeRing;
        transform.GetChild(0).localPosition = Vector3.up * 0.225f;
    }

    public void SetActive(bool b)
    {
        if (b) Activate();
        else Deactivate();
    }
}
