using UnityEngine;
using UnityEngine.Events;

public class HistoryState 
{
    byte turnNumber;
    byte priority;
    Dictionary<short, RobotState> robots;
    List<TileState> tiles;
    int myScore;
    int opponentScore;

    private class RobotState
    {
        public Vector3 position;
        public Vector3 rotation;
        public short health;
        public short attack;
        public List<CurrentEvent> currentEvents;
        public List<Cmd> commands;

        public class CurrentEvent
        {
            public Vector3 position;
            public Vector3 rotation;
            public Sprite sprite;
        }

        public class Cmd
        {
            public string name;
            public byte dir;
        }
    }

    private class TileState
    {
        public Material material;
    }

    public HistoryState(byte t, byte p)
    {
        turnNumber = t;
        priority = p;
    }

    public void SerializeRobots(Dictionary<short, RobotController> robotControllers)
    {
        robots = new Dictionary<short, RobotState>(robotControllers.GetLength());
        robotControllers.ForEachValue(SerializeRobot);
    }

    private void SerializeRobot(RobotController r)
    {
        RobotState state = new RobotState();
        state.position = r.transform.position;
        state.rotation = r.transform.rotation.eulerAngles;
        state.health = r.GetHealth();
        state.attack = r.GetAttack();
        state.currentEvents = r.currentEvents.Map(SerializeCurrentEvent);
        state.commands = r.commands.Map(SerializeCmd);
        robots.Add(r.id, state);
    }

    private RobotState.CurrentEvent SerializeCurrentEvent(SpriteRenderer s)
    {
        RobotState.CurrentEvent c = new RobotState.CurrentEvent();
        c.position = s.transform.position;
        c.rotation = s.transform.rotation.eulerAngles;
        c.sprite = s.sprite;
        return c;
    }

    private RobotState.Cmd SerializeCmd(Command cmd)
    {
        RobotState.Cmd c = new RobotState.Cmd();
        c.name = cmd.display;
        c.dir = cmd.direction;
        return c;
    }

    public void SerializeTiles(TileController[] tileControllers)
    {
        tiles = new List<TileController>(tileControllers).Map(SerializeTile);
    }

    private TileState SerializeTile(TileController t)
    {
        TileState state = new TileState();
        state.material = t.GetMaterial();
        return state;
    }

    public void SerializeScore(int mine, int opponents)
    {
        myScore = mine;
        opponentScore = opponents;
    }

    public void DeserializeRobots(Dictionary<short, RobotController> robotControllers, UnityAction<Command, short, bool> addCommandCallback)
    {
        robots.ForEach((id, state) => DeserializeRobot(robotControllers.Get(id), state, addCommandCallback));
    }

    private void DeserializeRobot(RobotController r, RobotState state, UnityAction<Command, short, bool> addCommandCallback)
    {
        r.transform.position = state.position;
        r.transform.rotation = Quaternion.Euler(state.rotation);
        r.displayHealth(state.health);
        r.displayAttack(state.attack);
        r.commands = new List<Command>();
        state.commands.ForEach(c => DeserializeCmd(c, r, addCommandCallback));
    }

    private void DeserializeCmd(RobotState.Cmd cmd, RobotController r, UnityAction<Command, short, bool> callback)
    {
        r.AddRobotCommand(cmd.name, cmd.dir, callback);
    }

    public void DeserializeTiles(TileController[] tileControllers)
    {
        new List<TileController>(tileControllers).ForEach(tiles, DeserializeTile);
    }

    private void DeserializeTile(TileController t, TileState state)
    {
        t.SetMaterial(state.material);
    }

    public void DeserializeScore(BoardController boardController)
    {
        boardController.SetBattery(myScore, opponentScore);
    }

    public bool IsBeforeOrDuring(byte p)
    {
        return p >= priority;
    }

    public bool IsAfter(byte p)
    {
        return p < priority;
    }
}
