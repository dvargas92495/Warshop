using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TileController : MonoBehaviour {

    public Sprite battery;

    public void OnMouseUp()
    {
        Interpreter.DestroyCommandMenu();
    }

    public void LoadTile(Map.Space.SpaceType spaceType)
    {
        switch (spaceType)
        {
            case Map.Space.SpaceType.VOID:
                gameObject.GetComponent<Image>().color = Color.black;
                break;
            case Map.Space.SpaceType.BLANK:
                gameObject.GetComponent<Image>().color = Color.white;
                break;
            case Map.Space.SpaceType.SPAWN:
                gameObject.GetComponent<Image>().color = Color.white;
                break;
            case Map.Space.SpaceType.PRIMARY_BASE:
                gameObject.GetComponent<Image>().sprite = battery;
                break;
            case Map.Space.SpaceType.SECONDARY_BASE:
                gameObject.GetComponent<Image>().sprite = battery;
                transform.Rotate(Vector3.forward * 180);
                break;
            case Map.Space.SpaceType.PRIMARY_QUEUE:
            case Map.Space.SpaceType.SECONDARY_QUEUE:
                gameObject.GetComponent<Image>().color = Color.yellow;
                break;
        }
    }

}
