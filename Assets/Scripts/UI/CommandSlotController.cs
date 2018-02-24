using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class CommandSlotController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {

    public Image Arrow;
    public Button Delete;
    public RectTransform Menu;
    public RectTransform Submenu;
    private bool clickable;
    internal bool deletable;
    internal bool isOpponent;

    private static Color NO_COMMAND = new Color(0.25f, 0.25f, 0.25f);
    private static Color HIGHLIGHTED_COMMAND = new Color(0.5f, 0.5f, 0.5f);
    private static Color SUBMITTED_COMMAND = new Color(0.75f, 0.75f, 0.75f);
    private static Color NEXT_COMMAND = new Color(0.5f, 1, 0.5f);
    private static Color OPEN_COMMAND = new Color(1, 1, 1);

    internal bool Clickable
    {
        get
        {
            return clickable;
        }

        set
        {
            clickable = value;
            if (value) Arrow.color = NEXT_COMMAND;
        }
    }

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

    public void OnPointerClick(PointerEventData eventData)
    {
        Interpreter.DestroyCommandMenu();
        Menu.gameObject.SetActive(Clickable && !isOpponent);
        Arrow.gameObject.SetActive(!Clickable || isOpponent);
    }

    internal void Initialize(short rid, int i, byte p)
    {
        if (i > p)
        {
            Arrow.color = NO_COMMAND;
        }
        Clickable = i == p;
        deletable = false;
        Delete.onClick.AddListener(() =>
        {
            Interpreter.DeleteCommand(rid, p - i);
            Delete.gameObject.SetActive(Arrow.sprite != null);
        });
        for (int j = 0; j < Menu.childCount - 1; j++)
        {
            MenuItemController menuItem = Menu.GetChild(j).GetComponent<MenuItemController>();
            menuItem.SetCallback(() =>
            {
                Submenu.gameObject.SetActive(true);
                Menu.gameObject.SetActive(false);
                for (int k = 0; k < Submenu.transform.childCount; k++)
                {
                    MenuItemController submenuitem = Submenu.GetChild(k).GetComponentInChildren<MenuItemController>();
                    Image image = submenuitem.GetComponent<Image>();
                    image.sprite = Interpreter.uiController.GetArrow(menuItem.name + " Arrow");
                    byte dir = Command.byteToDirectionString.First((KeyValuePair<byte, string> d) => d.Value.Equals(submenuitem.name)).Key;
                    submenuitem.SetCallback(() =>
                    {
                        if (menuItem.name.Equals(Command.Move.DISPLAY))
                        {
                            Interpreter.robotControllers[rid].addRobotCommand(new Command.Move(dir));
                        }
                        else if (menuItem.name.Equals(Command.Attack.DISPLAY))
                        {
                            Interpreter.robotControllers[rid].addRobotCommand(new Command.Attack(dir));
                        }
                        Submenu.gameObject.SetActive(false);
                        Arrow.gameObject.SetActive(true);
                    });
                    image.rectTransform.localRotation = Quaternion.Euler(Vector3.forward * dir * 90);
                }
            });
        }
    }

    internal void Open()
    {
        Arrow.color = OPEN_COMMAND;
        Arrow.rectTransform.rotation = Quaternion.Euler(Vector3.zero);
    }

    internal bool Opened()
    {
        return Arrow.color.Equals(OPEN_COMMAND);
    }

    internal bool Closed()
    {
        return Arrow.color.Equals(NO_COMMAND);
    }

    internal void Highlight()
    {
        Arrow.color = HIGHLIGHTED_COMMAND;
    }

    internal bool Highlighted()
    {
        return Arrow.color.Equals(HIGHLIGHTED_COMMAND);
    }

    internal void Submit()
    {
        Arrow.color = SUBMITTED_COMMAND;
        deletable = false;
        Clickable = false;
    }
}
