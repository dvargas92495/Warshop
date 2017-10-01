using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowHandButton : MonoBehaviour {

    private UIController UIController;

    public void buttonPress(){
        UIController = GameObject.Find("Controllers").GetComponent<UIController>();       
        UIController.ShowHandButtonPress();
    }

}
