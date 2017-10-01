using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubmitActionsButton : MonoBehaviour
{

    private UIController UIController;

    public void buttonPress()
    {
        UIController = GameObject.Find("Controllers").GetComponent<UIController>();
        UIController.SubmitActionsButtonPress();
    }

}
