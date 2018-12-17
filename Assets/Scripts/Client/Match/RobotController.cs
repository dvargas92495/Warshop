using UnityEngine;
using UnityEngine.Events;

public class RobotController : Controller
{
    public Animator animator;
    public AnimatorHelper animatorHelper;
    public GameObject defaultModel;
    public MeshRenderer healthMeshRenderer;
    public MeshRenderer attackMeshRenderer;
    public SpriteRenderer eventArrow;
    public TextMesh healthLabel;
    public TextMesh attackLabel;
    public GameObject[] robotModels;

    internal short id { get; private set; }
    internal bool isSpawned;
    internal bool isOpponent;
    internal List<SpriteRenderer> currentEvents = new List<SpriteRenderer>();
    internal List<Command> commands = new List<Command>();

    public void LoadModel(string n, short i)
    {
        id = i;
        GameObject model = new List<GameObject>(robotModels).Find(g => g.name.Equals(n));
        if (model == null) model = defaultModel;
        GameObject baseModel = Instantiate(model, animator.transform);
    }
    
    /***********************************
     * Robot Model Before Turn Methods *
     ***********************************/

    internal void AddRobotCommand(Command cmd, UnityAction<Command, short> callback)
    {
        int num = GetNumCommandType(cmd.commandId);
        if (num < Command.limit[cmd.commandId])
        {
            commands.Add(cmd);
            callback(cmd, id);
        }
    }

    private int GetNumCommandType(byte t)
    {
        return commands.Count(c => c.commandId == t);
    }

    internal void ShowMenuOptions(ButtonContainerController m)
    {
        if (!isSpawned &&commands.GetLength() == 0)
        {
            Util.ToList(Command.TYPES).ForEach(t => m.GetByName(Command.GetDisplay(t)).SetActive(t == Command.SPAWN_COMMAND_ID));
        }
        else
        {
            Util.ToList(Command.TYPES).ForEach(t =>
            {
                int num = GetNumCommandType(t);
                bool active = num < Command.limit[t] && !t.Equals(typeof(Command.Spawn));
                MenuItemController item = m.GetByName(Command.GetDisplay((byte)t));
                item.SetActive(active);
            });
        }
    }

    internal void AddRobotCommand(string name, byte dir, UnityAction<Command, short> callback)
    {
        if (name.Equals(Command.GetDisplay(Command.SPAWN_COMMAND_ID)))
        {
            AddRobotCommand(new Command.Spawn(dir), callback);
        }
        else if (name.Equals(Command.GetDisplay(Command.MOVE_COMMAND_ID)))
        {
            AddRobotCommand(new Command.Move(dir), callback);
        }
        else if (name.Equals(Command.GetDisplay(Command.ATTACK_COMMAND_ID)))
        {
            AddRobotCommand(new Command.Attack(dir), callback);
        }
    }

    /********************************
     * Robot UI During Turn Methods *
     ********************************/

    public void animate(string name, UnityAction interpreterCallback, UnityAction robotCallback)
    {
        animator.Play(name);
        animatorHelper.animatorCallback = () =>
        {
            robotCallback();
            interpreterCallback();
        };
    }

    public void displayMove(Vector2Int v, UnityAction callback, UnityAction<RobotController, int, int> robotCallback)
    {
        animate("Move" + getDir(v), callback, () => robotCallback(this, v.x, v.y));
    }

    public void displayAttack(Vector2Int v, UnityAction callback)
    {
        animate("Attack" + getDir(v), callback, () => {});
    }

    public void displaySpawn(Vector2Int v, UnityAction callback, UnityAction robotCallback)
    {
        animate("Default", callback, () => robotCallback());
    }

    public void displayDeath(short health, UnityAction callback, UnityAction robotCallback)
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
        healthLabel.text = health.ToString();
    }

    public void displayAttack(short attack)
    {
        attackLabel.text = attack.ToString();
    }

    public short GetHealth()
    {
        return short.Parse(healthLabel.text);
    }

    public short GetAttack()
    {
        return short.Parse(attackLabel.text);
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

    public SpriteRenderer displayEvent(Sprite eventName, Vector2Int targetLoc, bool relative = true)
    {
        Sprite eventType = eventName == null ? eventArrow.sprite : eventName;
        Vector3 loc = relative ? new Vector3((transform.position.x + targetLoc.x) / 2, (transform.position.y + targetLoc.y) / 2) : new Vector3(targetLoc.x, targetLoc.y);
        loc.z = -1;
        Quaternion rot = relative ? Quaternion.LookRotation(Vector3.forward, loc - transform.position) : Quaternion.identity;
        SpriteRenderer addedEvent = Instantiate(eventArrow, loc, rot, transform);
        addedEvent.sprite = eventType;
        addedEvent.sortingOrder += currentEvents.GetLength();
        currentEvents.Add(addedEvent);
        return addedEvent;
    }

    public void displayEvent(Sprite eventName, Vector2Int targetLoc, UnityAction callback, bool relative = true)
    {
        SpriteRenderer addedEvent = displayEvent(eventName, targetLoc, relative);
        // addedEvent.animator.Play("EventIndicator");
        // addedEvent.AnimatorHelper.animatorCallback = callback;
    }

    public void clearEvents()
    {
        currentEvents.ForEach(i => Destroy(i.gameObject));
        currentEvents = new List<SpriteRenderer>();
    }
}
