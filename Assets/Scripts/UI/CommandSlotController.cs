using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class CommandSlotController : MonoBehaviour {

    public SpriteRenderer Arrow;
    public SpriteRenderer Delete;
    internal bool deletable;
    internal bool isOpponent;

    private Action myClick;
    private static Color NO_COMMAND = new Color(0.25f, 0.25f, 0.25f);
    private static Color HIGHLIGHTED_COMMAND = new Color(0.5f, 0.5f, 0.5f);
    private static Color SUBMITTED_COMMAND = new Color(0.75f, 0.75f, 0.75f);
    private static Color NEXT_COMMAND = new Color(0.5f, 1, 0.5f);
    private static Color OPEN_COMMAND = new Color(1, 1, 1);

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnMouseEnter()
    {
        Delete.gameObject.SetActive(deletable);
    }

    void OnMouseExit()
    {
        Delete.gameObject.SetActive(false);
    }

    void OnMouseUp()
    {
        myClick();
    }

    internal void Initialize(short rid, int i, byte p)
    {
        if (i > p)
        {
            Arrow.color = NO_COMMAND;
        }else if (i == p)
        {
            Arrow.color = NEXT_COMMAND;
        }
        deletable = false;
        myClick = () =>
        {
            if (deletable)
            {
                Interpreter.DeleteCommand(rid, p - i);
                Interpreter.uiController.SetButtons(Interpreter.uiController.RobotButtonContainer, true);
                Interpreter.robotControllers.Values.ToList().ForEach((RobotController otherR) => 
                    Util.ChangeLayer(otherR.gameObject, Interpreter.uiController.BoardLayer)
                );
                Interpreter.uiController.SetButtons(Interpreter.uiController.CommandButtonContainer, false);
                Interpreter.uiController.SetButtons(Interpreter.uiController.DirectionButtonContainer, false);
                Interpreter.uiController.EachMenuItem(Interpreter.uiController.DirectionButtonContainer,
                    (MenuItemController m) => m.GetComponentInChildren<SpriteRenderer>().sprite = null
                );
                Interpreter.uiController.SubmitCommands.SetActive(
                    Interpreter.robotControllers.Values.Any((RobotController r) => r.commands.Count > 0)
                );
                deletable = !Arrow.sprite.Equals(Interpreter.uiController.Default);
                Delete.gameObject.SetActive(deletable);
            }
        };
    }

    internal void Open()
    {
        Arrow.color = OPEN_COMMAND;
        Arrow.transform.localRotation = Quaternion.identity;
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
    }

    internal void Next()
    {
        Arrow.color = NEXT_COMMAND;
    }

    internal bool IsNext()
    {
        return Arrow.color.Equals(NEXT_COMMAND);
    }
}
