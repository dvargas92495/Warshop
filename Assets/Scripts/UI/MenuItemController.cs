using System;
using UnityEngine;

public class MenuItemController : MonoBehaviour {

    Action callback;

    void OnMouseUp()
    {
        callback();
    }

    public void SetCallback(Action c)
    {
        callback = c;
    }
}
