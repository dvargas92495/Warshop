using UnityEngine;
using UnityEngine.Networking;

public class Robot
{
    internal readonly string name;
    internal readonly string description;
    internal byte priority;
    public short startingHealth;
    public short health;
    public short attack;
    internal Rating rating;
    public short id;
    public Vector2Int position;

    private static Logger log = new Logger(typeof(Robot).ToString());

    internal Robot(string _name, string _description)
    {
        name = _name;
        description = _description;
    }
    internal Robot(string _name, string _description, byte _priority, short _health, short _attack, Rating _rating)
    {
        name = _name;
        description = _description;
        priority = _priority;
        startingHealth = health = _health;
        attack = _attack;
        rating = _rating;
    }
    internal static Robot create(string robotName)
    {
        switch(robotName)
        {
            case Jaguar._name:
                return new Jaguar();
            case BronzeGrunt._name:
                return new BronzeGrunt();
            case SilverGrunt._name:
                return new SilverGrunt();
            case GoldenGrunt._name:
                return new GoldenGrunt();
            case PlatinumGrunt._name:
                return new PlatinumGrunt();
            default:
                log.Error("Invalid Robot name: " + robotName);
                return null;
        }
    }
    public void Serialize(NetworkWriter writer)
    {
        writer.Write(name);
        writer.Write(description);
        writer.Write(priority);
        writer.Write(health);
        writer.Write(attack);
        writer.Write((byte)rating);
        writer.Write(id);
        writer.Write(position.x);
        writer.Write(position.y);
    }
    public static Robot Deserialize(NetworkReader reader)
    {
        string _name = reader.ReadString();
        string _description = reader.ReadString();
        Robot robot = new Robot(_name, _description);
        robot.priority = reader.ReadByte();
        robot.health = reader.ReadInt16();
        robot.attack = reader.ReadInt16();
        robot.rating = (Rating)reader.ReadByte();
        robot.id = reader.ReadInt16();
        robot.position = new Vector2Int();
        robot.position.x = reader.ReadInt32();
        robot.position.y = reader.ReadInt32();
        return robot;
    }
    internal enum Rating
    {
        PLATINUM = 4,
        GOLD = 3,
        SILVER = 2,
        BRONZE = 1
    }
    internal virtual List<Vector2Int> GetVictimLocations(byte dir)
    {
        return Util.ToList(position + Command.DirectionToVector(dir));
    }

    internal virtual List<GameEvent> Spawn(Vector2Int pos, bool isPrimary)
    {
        GameEvent.Spawn evt = new GameEvent.Spawn();
        evt.destinationPos = pos;
        evt.primaryRobotId = id;
        evt.primaryBattery = isPrimary ? GameConstants.DEFAULT_SPAWN_POWER : (short)0;
        evt.secondaryBattery = isPrimary ? (short)0 : GameConstants.DEFAULT_SPAWN_POWER;
        return new List<GameEvent>(evt);
    }
    internal virtual List<GameEvent> Move(byte dir, bool isPrimary)
    {
        GameEvent.Move evt = new GameEvent.Move();
        evt.sourcePos = position;
        evt.destinationPos = position + Command.DirectionToVector(dir);
        evt.primaryRobotId = id;
        evt.primaryBattery = isPrimary ? GameConstants.DEFAULT_MOVE_POWER : (short)0;
        evt.secondaryBattery = isPrimary ? (short)0 : GameConstants.DEFAULT_MOVE_POWER;
        return new List<GameEvent>(evt);
    }
    internal virtual List<GameEvent> Attack(byte dir, bool isPrimary)
    {
        GameEvent.Attack evt = new GameEvent.Attack();
        evt.locs = GetVictimLocations(dir);
        evt.primaryRobotId = id;
        evt.primaryBattery = (isPrimary ? GameConstants.DEFAULT_ATTACK_POWER : (short)0);
        evt.secondaryBattery = (isPrimary ? (short)0 : GameConstants.DEFAULT_ATTACK_POWER);
        return new List<GameEvent>(evt);
    }
    internal virtual List<GameEvent> Damage(Robot victim)
    {
        GameEvent.Damage evt = new GameEvent.Damage();
        evt.primaryRobotId = victim.id;
        evt.damage = attack;
        evt.remainingHealth = (short)(victim.health - attack);
        return new List<GameEvent>(evt);
    }

    private class Jaguar : Robot
    {
        internal const string _name = "Jaguar";
        internal const string _description = "Can Move a Third Time (-1 Attack)";
        internal Jaguar() : base(
            _name,
            _description,
            6, 4, 4,
            Rating.SILVER
        )
        { }
        /*
        internal override List<GameEvent> CheckFail(Command c, Game.RobotTurnObject rto, bool isPrimary)
        {
            if (c is Command.Special)
            {
                return base.CheckFail(c, rto, isPrimary);
            }
            else if (c is Command.Move)
            {
                if (rto.num[typeof(Command.Attack)] == Command.limit[typeof(Command.Attack)])
                {
                    return base.CheckFail(c, rto, isPrimary);
                }
                else if (rto.num[c.GetType()] < Command.limit[c.GetType()] + 1)
                {
                    rto.num[c.GetType()]++;
                    return new List<GameEvent>();
                }
                else
                {
                    return new List<GameEvent>() { Fail(c, "of limit", isPrimary) };
                }
            } else
            {
                if (rto.num[typeof(Command.Move)] <= Command.limit[typeof(Command.Move)])
                {
                    return base.CheckFail(c, rto, isPrimary);
                }
                else
                {
                    return new List<GameEvent>() { Fail(c, "of limit", isPrimary) };
                }
            }
        }*/
    }

    private class BronzeGrunt : Robot
    {
        internal const string _name = "Bronze Grunt";
        internal const string _description = "No Ability";
        internal BronzeGrunt() : base(
            _name,
            _description,
            5, 8, 3,
            Rating.BRONZE
        )
        { }
    }

    private class SilverGrunt : Robot
    {
        internal const string _name = "Silver Grunt";
        internal const string _description = "No Ability";
        internal SilverGrunt() : base(
            _name,
            _description,
            6, 8, 3,
            Rating.SILVER
        )
        { }
    }

    private class GoldenGrunt : Robot
    {
        internal const string _name = "Golden Grunt";
        internal const string _description = "No Ability";
        internal GoldenGrunt(): base(
            _name,
            _description,
            7, 8, 3,
            Rating.GOLD
        )
        { }
    }

    private class PlatinumGrunt : Robot
    {
        internal const string _name = "Platinum Grunt";
        internal const string _description = "No Ability";
        internal PlatinumGrunt() : base(
            _name,
            _description,
            8, 8, 3,
            Rating.PLATINUM
        )
        { }
    }


}