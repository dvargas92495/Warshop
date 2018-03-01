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
    public GameObject menu;
    public GameObject submenu;
    public SpriteRenderer eventArrow;
    public TextMesh HealthLabel;
    public TextMesh AttackLabel;

    internal static RobotController robotBase;
    internal static Sprite[] robotDir;


    public static RobotController Load(Robot robot, Transform dock)
    {
        RobotController r = Instantiate(robotBase, dock);
        r.LoadModel(robot.name);
        r.name = robot.name;
        r.id = robot.id;
        r.displayHealth(robot.health);
        r.displayAttack(robot.attack);
        r.HealthLabel.GetComponent<MeshRenderer>().sortingOrder = r.HealthLabel.transform.parent.GetComponent<SpriteRenderer>().sortingOrder + 1;
        r.AttackLabel.GetComponent<MeshRenderer>().sortingOrder = r.AttackLabel.transform.parent.GetComponent<SpriteRenderer>().sortingOrder + 1;
        for (int i = 0; i < r.menu.transform.childCount; i++)
        {
            MenuItemController menuitem = r.menu.transform.GetChild(i).GetComponent<MenuItemController>();
            menuitem.SetCallback(() => r.toggleSubmenu(menuitem.name));
        }
        return r;
    }

    public void LoadModel(string n)
    {
        GameObject model = Instantiate(DefaultModel, transform);
        SpriteRenderer sprite = model.GetComponentInChildren<SpriteRenderer>();
        sprite.sprite = Array.Find(robotDir, (Sprite s) => s.name.Equals(n));
    }
    
    /************************
     * Robot Event Handlers *
     ************************/

    void FixedUpdate()
    {
        
    }

    private void OnMouseUp()
    {
        toggleMenu();
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
                m.transform.Find(Command.GetDisplay(t)).gameObject.SetActive(t.Equals(typeof(Command.Spawn)));
            });
        }
        else
        {
            Command.limit.Keys.ToList().ForEach((Type t) =>
            {
                int num = GetNumCommandType(t);
                bool active = num < Command.limit[t];
                MenuItemController item = m.transform.Find(Command.GetDisplay(t)).GetComponent<MenuItemController>();
                item.gameObject.SetActive(!t.Equals(typeof(Command.Spawn)));
                if (active) item.Activate();
                else item.Deactivate();
            });
            m.transform.Find(Command.Spawn.DISPLAY).gameObject.SetActive(false);
        }
        m.gameObject.SetActive(true);
    }

    internal void toggleMenu()
    {
        if (!canCommand) return;
        if (menu.activeInHierarchy)
        {
            menu.SetActive(false);
        } else
        {
            Interpreter.DestroyCommandMenu();
            ShowMenuOptions(menu);
        }
    }

    internal void toggleSubmenu(string command)
    {
        if (!canCommand) return;
        submenu.SetActive(true);
        menu.SetActive(false);
        bool isSpawn = command.Equals(Command.Spawn.DISPLAY);
        for (int i = 0; i < submenu.transform.childCount; i++)
        {
            MenuItemController submenuitem = submenu.transform.GetChild(i).GetComponent<MenuItemController>();
            submenuitem.GetComponent<SpriteRenderer>().sprite = command.Equals(Command.Spawn.DISPLAY) ?
                Interpreter.boardController.tile.queueSprites[i] : Interpreter.uiController.GetArrow(command + " Arrow");
            byte dir = Command.byteToDirectionString.First((KeyValuePair<byte, string> d) => d.Value.Equals(submenuitem.name)).Key;
            submenuitem.SetCallback(() =>
            {
                addRobotCommand(command, dir);
            });
            submenuitem.transform.localRotation = isSpawn ? Quaternion.identity : Quaternion.Euler(Vector3.forward * dir * 90);
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
        submenu.SetActive(false);
        toggleMenu();
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
        loc.z = menu.transform.position.z;
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
