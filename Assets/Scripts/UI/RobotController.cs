using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RobotController : MonoBehaviour
{
    public short id { get; protected set; }
    internal bool isOpponent;
    internal bool canCommand;
    internal bool inTurn;
    internal bool isMenuShown;
    internal List<SpriteRenderer> currentEvents = new List<SpriteRenderer>();
    internal List<Command> commands = new List<Command>();

    public MenuItemController menuItem;
    public SpriteRenderer eventArrow;
    public TextMesh HealthLabel;
    public TextMesh AttackLabel;
    public Sprite[] arrows;

    internal static RobotController robotBase;
    internal static Sprite[] robotDir;


    public static RobotController Load(Robot robot)
    {
        RobotController r = Instantiate(robotBase, Interpreter.boardController.transform);
        SpriteRenderer sprite = r.GetComponent<SpriteRenderer>();
        sprite.sprite = Array.Find(robotDir, (Sprite s) => s.name.Equals(robot.name));
        r.name = robot.name;
        r.id = robot.id;
        r.displayMove(robot.position);
        r.displayRotate(Robot.OrientationToVector(robot.orientation));
        r.displayHealth(robot.health);
        r.displayAttack(robot.attack);
        r.HealthLabel.GetComponent<MeshRenderer>().sortingOrder = r.HealthLabel.transform.parent.GetComponent<SpriteRenderer>().sortingOrder + 1;
        r.AttackLabel.GetComponent<MeshRenderer>().sortingOrder = r.AttackLabel.transform.parent.GetComponent<SpriteRenderer>().sortingOrder + 1;
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

    public void AddMoveCommand(byte dir)
    {
        addRobotCommand(new Command.Move(dir));
    }

    public void AddRotateCommand(byte dir)
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
        AddOptions(new List<Action>() {
            () => {
                AddOptions(new List<Action>(){
                    () => AddRotateCommand(Command.Rotate.CLOCKWISE),
                    () => AddRotateCommand(Command.Rotate.COUNTERCLOCKWISE),
                    () => AddRotateCommand(Command.Rotate.FLIP),
                },new List<string>(){
                    Command.Rotate.tostring[Command.Rotate.CLOCKWISE],
                    Command.Rotate.tostring[Command.Rotate.COUNTERCLOCKWISE],
                    Command.Rotate.tostring[Command.Rotate.FLIP],
                },true);
            },
            () => {
                AddOptions(new List<Action>(){
                    () => AddMoveCommand(Command.Move.LEFT),
                    () => AddMoveCommand(Command.Move.RIGHT),
                    () => AddMoveCommand(Command.Move.UP),
                    () => AddMoveCommand(Command.Move.DOWN)
                },new List<string>(){
                    Command.Move.tostring[Command.Move.LEFT],
                    Command.Move.tostring[Command.Move.RIGHT],
                    Command.Move.tostring[Command.Move.UP],
                    Command.Move.tostring[Command.Move.DOWN],
                },true);
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
        float menuH = transform.localScale.y;
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
            choice.GetComponentInChildren<MeshRenderer>().sortingOrder = choice.GetComponent<SpriteRenderer>().sortingOrder + 1;
            choice.transform.position += up*(menuH*itemH*(-i - 0.5f + (opts.Count/2.0f)));
            if (transform.position.x > (Interpreter.boardController.boardCellsWide/2) + Interpreter.boardController.transform.position.x)
            {
                choice.transform.position -= new Vector3(itemW * menuW * 0.5f, 0);
                if (isSubmenu) choice.transform.position -= new Vector3(itemW * menuW,0);
            }
            else
            {
                choice.transform.position += new Vector3(itemW * menuW * 0.5f, 0);
                if (isSubmenu) choice.transform.position += new Vector3(itemW * menuW, 0);
            }
            if (transform.position.y < Interpreter.boardController.boardCellsHeight/2)
            {
                choice.transform.position += Vector3.up*(((opts.Count/2.0f) - 0.5f) * itemH * menuH);
            }
            else
            {
                choice.transform.position -= Vector3.up*(((opts.Count/2.0f) - 0.5f) * itemH * menuH);
            }
            choice.isSubMenu = isSubmenu;
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
    
    public void displayHealth(short health)
    {
        HealthLabel.text = health.ToString();
    }

    public void displayAttack(short attack)
    {
        AttackLabel.text = attack.ToString();
    }

    public short GetHealth()
    {
        return short.Parse(HealthLabel.text);
    }

    public short GetAttack()
    {
        return short.Parse(AttackLabel.text);
    }

    public void displayEvent(string eventName, Vector2Int targetLoc)
    {
        Sprite eventType = Array.Find(arrows, (Sprite s) => s.name.Equals(eventName));
        Vector3 loc = new Vector3((transform.position.x + targetLoc.x) / 2, (transform.position.y + targetLoc.y) / 2);
        Quaternion rot = Quaternion.LookRotation(Vector3.forward, loc - transform.position);
        SpriteRenderer addedEvent = Instantiate(eventArrow, loc, rot, transform);
        addedEvent.sprite = eventType;
        currentEvents.Add(addedEvent);
    }

    public void clearEvents()
    {
        currentEvents.ForEach((SpriteRenderer i) => Destroy(i.gameObject));
        currentEvents.Clear();
    }
}
