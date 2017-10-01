using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CardController : MonoBehaviour {

    //model
    protected bool isOpponent;
    protected bool played;
    protected int power;
    protected int id;
    protected Archetype archetype;

    //ui
    protected bool selected;
    protected string displayName;
    protected string description;

    public abstract string getDisplayType();
    public abstract RobotCommand getCommand();

    private void OnMouseDown()
    {
        selectCard();
    }

    public int getId()
    {
        return id;
    }

    public int getPower()
    {
        return power;
    }

    public string getDisplayName()
    {
        return displayName;
    }

    public string getDescription()
    {
        return description;
    }

    public bool isSelected()
    {
        return selected;
    }

    public void selectCard()
    {
        if (!played)
        {
            selected = !selected;
            if (selected)
            {
                displaySelectCard();
            }
            else
            {
                displayUnselectCard();
            }
        }
    }

    public void chooseCard()
    {
        if (!played)
        {
            selectCard();
            played = true;
            displayChooseCard();
        }
    }

    private void displaySelectCard()
    {
        //TODO
    }

    private void displayUnselectCard()
    {
        //TODO
    }

    private void displayChooseCard()
    {
        //TODO
    }
}
