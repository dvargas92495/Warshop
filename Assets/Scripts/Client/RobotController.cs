using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

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
    public GameObject[] RobotModels;

    public void LoadModel(string n, short i)
    {
        id = i;
        GameObject model = Array.Find(RobotModels, (GameObject g) => g.name.Equals(n));
        if (model == null) model = DefaultModel;
        GameObject baseModel = Instantiate(model, transform.GetComponentInChildren<Animator>().transform);
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
            BaseGameManager.uiController.addSubmittedCommand(cmd, id);
        }
    }

    private int GetNumCommandType(Type t)
    {
        return commands.FindAll((Command c) => t.Equals(c.GetType())).Count;
    }

    internal void ShowMenuOptions(GameObject m)
    {
        if (//TODO
            //(transform.parent.Equals(BaseGameManager.boardController.myDock.transform) ||
            //transform.parent.Equals(BaseGameManager.boardController.opponentDock.transform)) &&
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

    public void animate(string name, Action interpreterCallback, Action robotCallback)
    {
        GetComponentInChildren<Animator>().Play(name);
        GetComponentInChildren<AnimatorHelper>().animatorCallback = () =>
        {
            robotCallback();
            interpreterCallback();
        };
    }

    public void displayMove(Vector2Int v, Action callback, UnityAction<RobotController, int, int> robotCallback)
    {
        animate("Move" + getDir(v), callback, () => robotCallback(this, v.x, v.y));
    }

    public void displayAttack(Vector2Int v, Action callback)
    {
        animate("Attack" + getDir(v), callback, () => {});
    }

    public void displaySpawn(Vector2Int v, Action callback, UnityAction robotCallback)
    {
        animate("Default", callback, () => robotCallback());
    }

    public void displayDeath(short health, Action callback, UnityAction robotCallback)
    {
        Debug.Log(id);
        animate("Death", callback, () => {
            displayHealth(health);
            gameObject.SetActive(false);
            robotCallback();
        });
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

    private string getDir(Vector2Int v)
    {
        string dir = "Reset";
        if (v - new Vector2Int((int)transform.position.x, (int)transform.position.y) == Vector2Int.left) dir = "Left";
        if (v - new Vector2Int((int)transform.position.x, (int)transform.position.y) == Vector2Int.right) dir = "Right";
        if (v - new Vector2Int((int)transform.position.x, (int)transform.position.y) == Vector2Int.up) dir = "Up";
        if (v - new Vector2Int((int)transform.position.x, (int)transform.position.y) == Vector2Int.down) dir = "Down";
        return dir;
    }

    public SpriteRenderer displayEvent(string eventName, Vector2Int targetLoc, bool relative = true)
    {
        Sprite eventType = eventName.Length == 0 ? GetComponentInChildren<SpriteRenderer>().sprite : BaseGameManager.uiController.GetArrow(eventName);
        Vector3 loc = relative ? new Vector3((transform.position.x + targetLoc.x) / 2, (transform.position.y + targetLoc.y) / 2) : new Vector3(targetLoc.x, targetLoc.y);
        loc.z = -1;
        Quaternion rot = relative ? Quaternion.LookRotation(Vector3.forward, loc - transform.position) : Quaternion.identity;
        SpriteRenderer addedEvent = Instantiate(eventArrow, loc, rot, transform);
        addedEvent.sprite = eventType;
        addedEvent.sortingOrder += currentEvents.Count;
        currentEvents.Add(addedEvent);
        return addedEvent;
    }

    public void displayEvent(string eventName, Vector2Int targetLoc, UnityAction callback, bool relative = true)
    {
        SpriteRenderer addedEvent = displayEvent(eventName, targetLoc, relative);
        addedEvent.GetComponent<Animator>().Play("EventIndicator");
        addedEvent.GetComponent<AnimatorHelper>().animatorCallback = callback;
    }

    public void clearEvents()
    {
        currentEvents.ForEach((SpriteRenderer i) => Destroy(i.gameObject));
        currentEvents.Clear();
    }
}
