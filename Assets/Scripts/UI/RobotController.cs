using System;
using System.Collections.Generic;
using UnityEngine;

public class RobotController : MonoBehaviour
{
    //model
    protected int attack;
    protected int health;
    protected int priority;
    public short id { get; protected set; }
    internal bool isOpponent;
    internal bool canCommand;
    protected bool equipped;
    protected Vector2 position;
    protected Vector2 orientation;
    protected List<Command> commands = new List<Command>();
    
    protected bool inTurn;
    internal bool isMenuShown;
    protected string displayName;
    protected string displayAbbreviation;
    protected string description;


    public static RobotController Make(Robot robot)
    {
        RobotController robotController = Instantiate(Resources.Load<GameObject>(GameConstants.ROBOT_PREFAB_DIR + robot.name)).GetComponent<RobotController>();
        robotController.name = robot.name;
        robotController.id = robot.id;
        robotController.health = robot.health;
        robotController.attack = robot.attack;
        robotController.priority = robot.priority;
        robotController.position = robot.position;
        robotController.orientation = Robot.OrientationToVector(robot.orientation);
        robotController.displayMove();
        robotController.displayRotate();
        return robotController;
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

    public int GetAttack()
    {
        return attack;
    }

    public int GetHealth()
    {
        return health;
    }

    public int GetPriority()
    {
        return priority;
    }

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
        Logger.ClientLog("TODO: Update " + name + "'s health to " + h + " on UI");
    }

    public void RotateCounterclockwise()
    {
        orientation = new Vector2(-orientation.y, orientation.x);
        displayRotate();
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
        GameObject menuItem = Resources.Load<GameObject>(GameConstants.ROBOT_MENU_PREFAB);
        int midx = Interpreter.boardController.boardCellsWide / 2;
        int midy = Interpreter.boardController.boardCellsHeight / 2;
        int x = (isSubmenu ? 4 : 1);
        int xmult = (position.x < midx ? 1 : -1);
        int xoffset = (position.x < midx ? 0 : -3);
        int yoffset = (position.y < midy ? 0 : -opts.Count);
        for (int i = 0; i < opts.Count; i++)
        {
            GameObject choice = Instantiate(menuItem, transform);
            choice.transform.localPosition = new Vector3(x*xmult + xoffset, i + yoffset, 0);
            choice.transform.RotateAround(transform.position, new Vector3(0, 0, 1), -getAngle());
            MenuItemController script = choice.GetComponent<MenuItemController>();
            script.SetCallback(opts[i]);
            choice.GetComponent<TextMesh>().text = vals[i];
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
        gameObject.transform.rotation = Quaternion.Euler(0, 0, getAngle());
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
