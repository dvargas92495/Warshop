using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Z8.Generic;

public class RobotController : MonoBehaviour
{
    //model
    protected int attack;
    protected int health;
    protected int priority;
    protected int id;
    protected bool isOpponent;
    protected bool equipped;
    protected Vector2 position;
    protected Vector2 orientation;
    protected List<RobotCommand> commands = new List<RobotCommand>();
    protected List<MenuOptions> options = new List<MenuOptions>//TODO:tmp
    {
        MenuOptions.SPAWN,
        MenuOptions.MOVE,
        MenuOptions.ROTATE,
        MenuOptions.ATTACK,
    };

    //ui
    protected bool inTurn;
    protected bool isMenuShown;
    protected string displayName;
    protected string displayAbbreviation;
    protected string description;


    public static void Make(RobotObject robot)
    {
        RobotController robotController = Instantiate(Resources.Load<GameObject>(GameConstants.ROBOT_PREFAB_DIR + robot.Name)).GetComponent<RobotController>();
        robotController.name = robot.Identifier;
        robotController.id = robot.Id;
        robotController.health = robot.Health;
        robotController.attack = robot.Attack;
        robotController.priority = robot.Priority;
        robotController.isOpponent = robot.IsOpponent;
        robotController.orientation = (robot.IsOpponent ? new Vector2(-1, 0) : new Vector2(1, 0));
        robotController.displayRotate();
    }
    
    /************************
     * Robot Event Handlers *
     ************************/

    void FixedUpdate()
    {
        if (GameConstants.LOCAL_MODE && isMenuShown)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                MoveForward();
            }
            if (Input.GetKeyUp(KeyCode.DownArrow))
            {
                MoveBackward();
            }
            if (Input.GetKeyUp(KeyCode.LeftArrow))
            {
                MoveLeft();
            }
            if (Input.GetKeyUp(KeyCode.RightArrow))
            {
                MoveRight();
            }
            if (Input.GetKeyUp(KeyCode.A))
            {
                RotateClockwise();
            }
            if (Input.GetKeyUp(KeyCode.D))
            {
                RotateCounterclockwise();
            }
        }
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

    public List<RobotCommand> GetCommands()
    {
        return commands;
    }

    public void ClearRobotCommands()
    {
        commands.Clear();
    }

    public void AddSpawnCommand(int x, int y)
    {
        addRobotCommand(new SpawnCommand(x, y));
    }

    public void AddMoveCommand(MoveCommand.Direction dir)
    {
        addRobotCommand(new MoveCommand(dir));
    }

    public void AddRotateCommand(RotateCommand.Direction dir)
    {
        addRobotCommand(new RotateCommand(dir));
    }

    public void AddAttackCommand()
    {
        addRobotCommand(new AttackCommand());
    }

    private void addRobotCommand(RobotCommand cmd)
    {
        commands.Add(cmd);
        displayAddingRobotCommand(cmd, this.gameObject.name);
        toggleMenu();
    }

    private void toggleMenu()
    {
        RobotController[] allRobots = FindObjectsOfType<RobotController>();
        foreach (RobotController robot in allRobots)
        {
            if (robot.isMenuShown && !(robot.id == id && robot.isOpponent == isOpponent))
            {
                robot.toggleMenu();
                break;
            }
        }
        isMenuShown = !isMenuShown;
        if (isMenuShown)
        {
            displayShowMenu();
        }
        else
        {
            displayHideMenu(false);
        }
    }

    /***********************************
     * Robot Model During Turn Methods *
     ***********************************/

    public void MoveForward()
    {
        position += orientation;
        displayMove();
    }

    public void MoveBackward()
    {
        position -= orientation;
        displayMove();
    }

    public void MoveRight()
    {
        position += new Vector2(-orientation.y, orientation.x);
        displayMove();
    }

    public void MoveLeft()
    {
        position += new Vector2(orientation.y, -orientation.x);
        displayMove();
    }

    public void Place(int x, int y)
    {
        position = new Vector2(x, y);
        displayMove();
    }

    public void RotateClockwise()
    {
        orientation = new Vector2(orientation.y, -orientation.x);
        displayRotate();
    }
    
    public void RotateCounterclockwise()
    {
        orientation = new Vector2(-orientation.y, orientation.x);
        displayRotate();
    }

    public int GetId()
    {
        return id;
    }

    public bool IsOpponent()
    {
        return isOpponent;
    }

    /********************************
     * Robot UI Before Turn Methods *
     ********************************/

    private void displayAddingRobotCommand(RobotCommand cmd, string robotIdentifer)
    {
        GameObject Controllers = GameObject.Find("Controllers");
        Controllers.GetComponent<UIController>().addSubmittedCommand(cmd, robotIdentifer); 
        
    }

    private void displayShowMenu()
    {
        AddOptions(options, options.ConvertAll(x => RobotMenuController.LABELS[x]),false);
    }

    private void displayHideMenu(bool onlySub)
    {
        RobotMenuController[] menuItems = FindObjectsOfType<RobotMenuController>();
        foreach(RobotMenuController menuItem in menuItems)
        {
            if (!onlySub || RobotMenuController.LABELS[menuItem.GetOption()] != menuItem.GetValue()) //hacky
            {
                Destroy(menuItem.gameObject);
            }
        }
    }

    public void ClickMenuOption(MenuOptions option, string value)
    {
        BoardController board = FindObjectOfType<BoardController>();
        switch (option)
        {
            case MenuOptions.ATTACK:
                AddAttackCommand();
                break;
            case MenuOptions.MOVE:
                switch (value)
                {
                    case RobotMenuController.MOVE:
                        displayHideMenu(true);
                        List<string> vals = new List<string>(MoveCommand.LABELTOENUM.Keys);
                        AddOptions(vals.ConvertAll(x => MenuOptions.MOVE), vals, true);
                        break;
                    case MoveCommand.FORWARD:
                    case MoveCommand.BACK:
                    case MoveCommand.LEFT:
                    case MoveCommand.RIGHT:
                        AddMoveCommand(MoveCommand.LABELTOENUM[value]);
                        break;

                }
                break;
            case MenuOptions.ROTATE:
                switch (value)
                {
                    case RobotMenuController.ROTATE:
                        displayHideMenu(true);
                        List<string> vals = new List<string>(RotateCommand.LABELTOENUM.Keys);
                        AddOptions(vals.ConvertAll(x => MenuOptions.ROTATE), vals, true);
                        break;
                    case RotateCommand.NORTH:
                    case RotateCommand.SOUTH:
                    case RotateCommand.EAST:
                    case RotateCommand.WEST:
                        AddRotateCommand(RotateCommand.LABELTOENUM[value]);
                        break;

                }
                break;
            case MenuOptions.SPAWN:
                switch (value)
                {
                    case RobotMenuController.SPAWN:
                        displayHideMenu(true);
                        List<string> vals = new List<string> ();
                        for (int i = 1;i <= board.GetNumSpawnLocations(isOpponent); i++)
                        {
                            vals.Add(i.ToString());
                        }
                        AddOptions(vals.ConvertAll(x => MenuOptions.SPAWN), vals, true);
                        break;
                    default:
                        int[] coords = board.GetSpawn(int.Parse(value) - 1, isOpponent);
                        AddSpawnCommand(coords[0], coords[1]);
                        break;
                }
                break;
        }
    }

    private void AddOptions(List<MenuOptions> opts, List<string> vals, bool isSubmenu)
    {
        GameObject menuItem = Resources.Load<GameObject>(GameConstants.ROBOT_MENU_PREFAB);
        int x = (isSubmenu ? 4 : 1);
        int xmult = (position.x < 5 ? 1 : -1); //TODO 5 should be boardwidth/2
        int xoffset = (position.x < 5 ? 0 : -3); //TODO 5 should be boardwidth/2
        int yoffset = (position.y < 5 ? 0 : -opts.Count); //TODO 5 should be boardheight/2
        for (int i = 0; i < opts.Count; i++)
        {
            GameObject choice = Instantiate(menuItem, transform);
            choice.transform.localPosition = new Vector3(x*xmult + xoffset, i + yoffset, 0);
            choice.transform.RotateAround(transform.position, new Vector3(0, 0, 1), -getAngle());
            RobotMenuController script = choice.GetComponent<RobotMenuController>();
            script.SetOptionRobotValue(opts[i], this, vals[i]);
            choice.GetComponent<TextMesh>().text = script.GetValue();
        }
    }

    private void DebugCommands()
    {
        string owner = (isOpponent ? "Opponent's " : "My ");
        Debug.Log(owner + displayName + " Commands:");
        foreach (RobotCommand cmd in commands)
        {
            Debug.Log(cmd.toString());
        }
    }

    /********************************
     * Robot UI During Turn Methods *
     ********************************/

    private void displayMove()
    {
        BoardController board = FindObjectOfType<BoardController>();
        board.PlaceRobot(transform, (int) position.x, (int) position.y);
    }

    private void displayRotate()
    {
        gameObject.transform.rotation = Quaternion.Euler(0, 0, getAngle());
    }

    private float getAngle()
    {
        //I can't do math
        float angle = 0;
        if (orientation.x == 1) { angle = 90; }
        else if (orientation.x == -1) { angle = 270; }
        else if (orientation.y == 1) { angle = 180; }
        else if (orientation.y == -1) { angle = 0; }
        return angle;
    }
    

}
