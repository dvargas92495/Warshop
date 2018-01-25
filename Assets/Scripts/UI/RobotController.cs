using System;
using System.Collections.Generic;
using UnityEngine;

public class RobotController : MonoBehaviour
{
    //model
    internal int attack;
    internal int health;
    internal int priority;
    public short id { get; protected set; }
    internal bool isOpponent;
    internal bool canCommand;
    protected bool equipped;
    protected Vector2 position;
    protected Vector2 orientation;
    protected List<Command> commands = new List<Command>();

    public MenuItemController menuItem;
    protected bool inTurn;
    internal bool isMenuShown;
    protected string displayName;
    protected string displayAbbreviation;
    protected string description;


    public void Load(Robot robot)
    {
        name = robot.name;
        id = robot.id;
        health = robot.health;
        attack = robot.attack;
        priority = robot.priority;
        position = robot.position;
        orientation = Robot.OrientationToVector(robot.orientation);
        displayMove();
        displayRotate();
    }
    
    /************************
     * Robot Event Handlers *
     ************************/

    void FixedUpdate()
    {
        
    }

    void OnMouseUp()
    {
        toggleMenu();
    }

    /***********************************
     * Robot Model Before Turn Methods *
     ***********************************/

    public string GetDisplyName()
    {
        return displayName;
    }

    public string GetDescription()
    {
        return description;
    }

    public string GetAbbreviation()
    {
        return displayAbbreviation;
    }

    public List<Command> GetCommands()
    {
        return commands;
    }

    public void ClearRobotCommands()
    {
        commands.Clear();
    }

    public void AddMoveCommand(Command.Direction dir)
    {
        addRobotCommand(new Command.Move(dir));
    }

    public void AddRotateCommand(Command.Direction dir)
    {
        addRobotCommand(new Command.Rotate(dir));
    }

    public void AddAttackCommand()
    {
        addRobotCommand(new Command.Attack());
    }

    private void addRobotCommand(Command cmd)
    {
        commands.Add(cmd);
        displayAddingRobotCommand(cmd, gameObject.name);
        toggleMenu();
    }

    private void toggleMenu()
    {
        if (!canCommand) return;
        if (isMenuShown)
        {
            isMenuShown = false;
            displayHideMenu(false);
        } else
        {
            Interpreter.DestroyCommandMenu();
            isMenuShown = true;
            displayShowMenu();
        }
    }

    /***********************************
     * Robot Model During Turn Methods *
     ***********************************/

    public void Place(int x, int y)
    {
        position = new Vector2(x, y);
        displayMove();
    }

    public void Rotate(Vector2 orient)
    {
        orientation = orient;
        displayRotate();
    }
    
    public void SetHealth(int h)
    {
        health = h;
        Interpreter.uiController.UpdateAttributes(this);
    }

    /********************************
     * Robot UI Before Turn Methods *
     ********************************/

    private void displayAddingRobotCommand(Command cmd, string robotIdentifer)
    {
        Interpreter.uiController.addSubmittedCommand(cmd, robotIdentifer); 
    }

    private void displayShowMenu()
    {
        List<String> dirList = new List<string>(){
            Command.Direction.LEFT.ToString(),
            Command.Direction.RIGHT.ToString(),
            Command.Direction.UP.ToString(),
            Command.Direction.DOWN.ToString()
        };
        AddOptions(new List<Action>() {
            () => {
                AddOptions(new List<Action>(){
                    () => AddRotateCommand(Command.Direction.LEFT),
                    () => AddRotateCommand(Command.Direction.RIGHT),
                    () => AddRotateCommand(Command.Direction.UP),
                    () => AddRotateCommand(Command.Direction.DOWN)
                },dirList,true);
            },
            () => {
                AddOptions(new List<Action>(){
                    () => AddMoveCommand(Command.Direction.LEFT),
                    () => AddMoveCommand(Command.Direction.RIGHT),
                    () => AddMoveCommand(Command.Direction.UP),
                    () => AddMoveCommand(Command.Direction.DOWN)
                },dirList,true);
            },
            () => {
                AddAttackCommand();
            },
        }, new List<string> {
            Command.Rotate.DISPLAY,
            Command.Move.DISPLAY,
            Command.Attack.DISPLAY
        },false);
    }

    public void displayHideMenu(bool onlySub)
    {
        MenuItemController[] menuItems = FindObjectsOfType<MenuItemController>();
        foreach(MenuItemController menuItem in menuItems)
        {
            if (!onlySub) //hacky
            {
                Destroy(menuItem.gameObject);
            }
        }
    }

    private void AddOptions(List<Action> opts, List<string> vals, bool isSubmenu)
    {
        float menuW = transform.localScale.x;
        float itemW = menuItem.transform.localScale.x;
        float itemH = menuItem.transform.localScale.y;
        Vector3 up = Interpreter.uiController.boardCamera.transform.up;
        for (int i = 0; i < opts.Count; i++)
        {
            MenuItemController choice = Instantiate(menuItem, transform);
            choice.transform.position = transform.position;
            choice.transform.rotation = Interpreter.uiController.boardCamera.transform.rotation;
            choice.SetCallback(opts[i]);
            choice.GetComponentInChildren<TextMesh>().text = vals[i];
            choice.transform.position += up*(itemH*(-i - 0.5f + (opts.Count/2.0f)));
            if (transform.position.x > Interpreter.uiController.boardCamera.transform.position.x)
            {
                choice.transform.position -= new Vector3(itemW / 2 + menuW / 2, 0);
                if (isSubmenu) choice.transform.position -= new Vector3(itemW,0);
            }
            else
            {
                choice.transform.position += new Vector3(itemW / 2 + menuW / 2, 0);
                if (isSubmenu) choice.transform.position += new Vector3(itemW, 0);
            }
            float dify = transform.position.y - Interpreter.uiController.boardCamera.transform.position.y;
            if (transform.position.y < Interpreter.uiController.boardCamera.transform.position.y)
            {
                choice.transform.position += Vector3.up*(((opts.Count/2.0f) - 0.5f) * itemH);
            }
            else
            {
                choice.transform.position -= Vector3.up*(((opts.Count/2.0f) - 0.5f) * itemH);
            }
        }

    }

    private void DebugCommands()
    {
        string owner = (isOpponent ? "Opponent's " : "My ");
        Debug.Log(owner + displayName + " Commands:");
        foreach (Command cmd in commands)
        {
            Debug.Log(cmd.ToString());
        }
    }

    /********************************
     * Robot UI During Turn Methods *
     ********************************/

    private void displayMove()
    {
        Interpreter.boardController.PlaceRobot(transform, (int) position.x, (int) position.y);
    }

    private void displayRotate()
    {
        transform.rotation = Quaternion.LookRotation(Vector3.back, orientation);
    }

    private float getAngle()
    {
        //I can't do math
        float angle = 0;
        if (orientation.x == 1) { angle = 270; }
        else if (orientation.x == -1) { angle = 90; }
        else if (orientation.y == 1) { angle = 0; }
        else if (orientation.y == -1) { angle = 180; }
        return angle;
    }
    

}
