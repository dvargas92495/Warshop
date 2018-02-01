using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RobotController : MonoBehaviour
{
    public short id { get; protected set; }
    internal bool isOpponent;
    internal bool canCommand;
    protected List<Command> commands = new List<Command>();

    public MenuItemController menuItem;
    internal static RobotController robotBase;
    internal static Sprite[] robotDir;
    protected bool inTurn;
    internal bool isMenuShown;
    protected string displayName;
    protected string displayAbbreviation;
    protected string description;


    public static RobotController Load(Robot robot)
    {
        RobotController r = Instantiate(robotBase, Interpreter.boardController.transform);
        Image sprite = r.GetComponent<Image>();
        sprite.sprite = Array.Find(robotDir, (Sprite s) => s.name.Equals(robot.name));
        r.name = robot.name;
        r.id = robot.id;
        r.displayMove(robot.position);
        r.displayRotate(Robot.OrientationToVector(robot.orientation));
        return r;
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
        Interpreter.uiController.addSubmittedCommand(cmd, id);
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

    /********************************
     * Robot UI Before Turn Methods *
     ********************************/

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
        if (isSubmenu)
        {
            Array.ForEach(GetComponentsInChildren<MenuItemController>(), (MenuItemController mi) =>
            {
                if (mi.isSubMenu) Destroy(mi.gameObject);
            });
        }
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
            if (transform.position.x > (Interpreter.boardController.boardCellsWide/2) + Interpreter.boardController.transform.position.x)
            {
                choice.transform.position -= new Vector3(itemW / 2 + menuW / 2, 0);
                if (isSubmenu) choice.transform.position -= new Vector3(itemW,0);
            }
            else
            {
                choice.transform.position += new Vector3(itemW / 2 + menuW / 2, 0);
                if (isSubmenu) choice.transform.position += new Vector3(itemW, 0);
            }
            if (transform.position.y < Interpreter.boardController.boardCellsHeight/2)
            {
                choice.transform.position += Vector3.up*(((opts.Count/2.0f) - 0.5f) * itemH);
            }
            else
            {
                choice.transform.position -= Vector3.up*(((opts.Count/2.0f) - 0.5f) * itemH);
            }
            choice.isSubMenu = isSubmenu;
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

    public void displayMove(Vector2Int v)
    {
        Interpreter.boardController.PlaceRobot(transform, v.x, v.y);
    }

    public void displayRotate(Vector2Int v)
    {
        transform.rotation = Quaternion.LookRotation(Vector3.forward, new Vector3(v.x, v.y));
    }
    

}
