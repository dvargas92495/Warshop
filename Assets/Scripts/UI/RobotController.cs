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
    internal List<SpriteRenderer> currentEvents = new List<SpriteRenderer>();
    internal List<Command> commands = new List<Command>();

    public GameObject menu;
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
        for (int i = 0; i < r.menu.transform.childCount; i++)
        {
            MenuItemController menuitem = r.menu.transform.GetChild(i).GetComponent<MenuItemController>();
            switch (menuitem.name)
            {
                case "MoveUp":
                    menuitem.SetCallback(() => r.AddMoveCommand(Command.Move.UP));
                    break;
                case "MoveDown":
                    menuitem.SetCallback(() => r.AddMoveCommand(Command.Move.DOWN));
                    break;
                case "MoveLeft":
                    menuitem.SetCallback(() => r.AddMoveCommand(Command.Move.LEFT));
                    break;
                case "MoveRight":
                    menuitem.SetCallback(() => r.AddMoveCommand(Command.Move.RIGHT));
                    break;
                case "RotateClockwise":
                    menuitem.SetCallback(() => r.AddRotateCommand(Command.Rotate.CLOCKWISE));
                    break;
                case "RotateCounterclockwise":
                    menuitem.SetCallback(() => r.AddRotateCommand(Command.Rotate.COUNTERCLOCKWISE));
                    break;
                case "RotateFlip":
                    menuitem.SetCallback(() => r.AddRotateCommand(Command.Rotate.FLIP));
                    break;
                case "Attack":
                    menuitem.SetCallback(() => r.AddAttackCommand());
                    break;
                case "Special":
                    menuitem.SetCallback(() => r.toggleMenu());
                    break;
            }
        }
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
        Interpreter.uiController.addSubmittedCommand(GetArrow(cmd.ToString()), id);
    }

    private void toggleMenu()
    {
        if (!canCommand) return;
        if (menu.activeInHierarchy)
        {
            menu.SetActive(false);
        } else
        {
            Interpreter.DestroyCommandMenu();
            menu.SetActive(true);
            menu.transform.rotation = Quaternion.LookRotation(Vector3.forward, Interpreter.uiController.boardCamera.transform.up);
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

    public void displayEvent(string eventName, Vector2Int targetLoc, bool avg = true)
    {
        Sprite eventType = GetArrow(eventName);
        Vector3 loc = avg ? new Vector3((transform.position.x + targetLoc.x) / 2, (transform.position.y + targetLoc.y) / 2) : new Vector3(targetLoc.x, targetLoc.y);
        Quaternion rot = Quaternion.LookRotation(Vector3.forward, loc - transform.position);
        SpriteRenderer addedEvent = Instantiate(eventArrow, loc, rot, transform);
        addedEvent.sprite = eventType;
        addedEvent.sortingOrder += currentEvents.Count;
        currentEvents.Add(addedEvent);
    }

    public void clearEvents()
    {
        currentEvents.ForEach((SpriteRenderer i) => Destroy(i.gameObject));
        currentEvents.Clear();
    }

    public Sprite GetArrow(string eventName)
    {
        return Array.Find(arrows, (Sprite s) => s.name.Equals(eventName));
    }
}
