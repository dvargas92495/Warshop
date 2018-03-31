using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RobotController : MonoBehaviour
{
    public short id { get; protected set; }
    internal bool isOpponent;
    internal bool canCommand;
    internal List<SpriteRenderer> currentEvents = new List<SpriteRenderer>();
    internal List<Command> commands = new List<Command>();

    public GameObject DefaultModel;
    public SpriteRenderer eventArrow;
    public TextMesh HealthLabel;
    public TextMesh AttackLabel;

    public static RobotController Load(Robot robot, Transform dock)
    {
        RobotController r = Instantiate(Interpreter.boardController.robotBase, dock);
        r.LoadModel(robot.name);
        r.name = robot.name;
        r.id = robot.id;
        r.displayHealth(robot.health);
        r.displayAttack(robot.attack);
        r.HealthLabel.GetComponent<MeshRenderer>().sortingOrder = r.HealthLabel.transform.parent.GetComponent<SpriteRenderer>().sortingOrder + 1;
        r.AttackLabel.GetComponent<MeshRenderer>().sortingOrder = r.AttackLabel.transform.parent.GetComponent<SpriteRenderer>().sortingOrder + 1;
        return r;
    }

    public void LoadModel(string n)
    {
        GameObject model = Array.Find(Interpreter.boardController.RobotModels, (GameObject g) => g.name.Equals(n));
        if (model == null) model = DefaultModel;
        GameObject baseModel = Instantiate(model, transform);
        baseModel.transform.Rotate(Vector3.left * 90);
    }
    
    /***********************************
     * Robot Model Before Turn Methods *
     ***********************************/

    internal void addRobotCommand(Command cmd)
    {
        int num = GetNumCommandType(cmd.GetType());
        if (num < Command.limit[cmd.GetType()])
        {
            commands.Add(cmd);
            Interpreter.uiController.addSubmittedCommand(cmd, id);
        }
    }

    private int GetNumCommandType(Type t)
    {
        return commands.FindAll((Command c) => t.Equals(c.GetType())).Count;
    }

    internal void ShowMenuOptions(GameObject m)
    {
        if (
            (transform.parent.Equals(Interpreter.boardController.primaryDock.transform) ||
            transform.parent.Equals(Interpreter.boardController.secondaryDock.transform)) &&
            commands.Count == 0
        )
        {
            Command.limit.Keys.ToList().ForEach((Type t) =>
            {
                m.transform.Find(Command.GetDisplay(t)).GetComponent<MenuItemController>().SetActive(t.Equals(typeof(Command.Spawn)));
            });
        }
        else
        {
            Command.limit.Keys.ToList().ForEach((Type t) =>
            {
                int num = GetNumCommandType(t);
                bool active = num < Command.limit[t] && !t.Equals(typeof(Command.Spawn));
                MenuItemController item = m.transform.Find(Command.GetDisplay(t)).GetComponent<MenuItemController>();
                item.SetActive(active);
            });
        }
    }

    internal void addRobotCommand(string name, byte dir)
    {
        if (name.Equals(Command.Spawn.DISPLAY))
        {
            addRobotCommand(new Command.Spawn(dir));
        }
        else if (name.Equals(Command.Move.DISPLAY))
        {
            addRobotCommand(new Command.Move(dir));
        }
        else if (name.Equals(Command.Attack.DISPLAY))
        {
            addRobotCommand(new Command.Attack(dir));
        }
    }

    /********************************
     * Robot UI During Turn Methods *
     ********************************/

    public void displayMove(Vector2Int v)
    {
        Interpreter.boardController.PlaceRobot(transform, v.x, v.y);
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

    public void displayEvent(string eventName, Vector2Int targetLoc, bool relative = true)
    {
        Sprite eventType = eventName.Length == 0 ? GetComponentInChildren<SpriteRenderer>().sprite : Interpreter.uiController.GetArrow(eventName);
        Vector3 loc = relative ? new Vector3((transform.position.x + targetLoc.x) / 2, (transform.position.y + targetLoc.y) / 2) : new Vector3(targetLoc.x, targetLoc.y);
        loc.z = -1;
        Quaternion rot = relative ? Quaternion.LookRotation(Vector3.forward, loc - transform.position) : Quaternion.identity;
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
}
