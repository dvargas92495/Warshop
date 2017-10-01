using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowQueuedActionsButton : MonoBehaviour
{

    private UIController UIController;

    public void buttonPress()
    {
        UIController = GameObject.Find("Controllers").GetComponent<UIController>();
        UIController.ShowQueuedActionsButtonPress();
    }

}
