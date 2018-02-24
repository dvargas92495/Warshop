using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CommandSlotController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

    public Image RobotImage;
    public Button Delete;
    internal bool deletable;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnPointerEnter(PointerEventData eventData)
    {
        Delete.gameObject.SetActive(deletable);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Delete.gameObject.SetActive(false);
    }
}
