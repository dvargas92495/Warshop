using UnityEngine;
using UnityEngine.Networking;

public class Robot
{
    public readonly string name;
    public readonly string description;
    public byte priority;
    public short startingHealth;
    public short health;
    public short attack;
    public Rating rating;
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

    public override string ToString()
    {
        return "Robot: " + name + " - " + description + " (" + attack + "," + health + ")";
    }

    internal static Robot create(string robotName)
    {
        switch(robotName)
        {
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
    public enum Rating
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
        SpawnEvent evt = new SpawnEvent();
        evt.destinationPos = pos;
        evt.robotId = id;
        if (isPrimary) evt.primaryBatteryCost = GameConstants.DEFAULT_SPAWN_POWER;
        else evt.secondaryBatteryCost = GameConstants.DEFAULT_SPAWN_POWER;
        return new List<GameEvent>(evt);
    }
    internal virtual List<GameEvent> Move(byte dir, bool isPrimary)
    {
        MoveEvent evt = new MoveEvent();
        evt.sourcePos = position;
        evt.destinationPos = position + Command.DirectionToVector(dir);
        evt.robotId = id;
        if (isPrimary) evt.primaryBatteryCost = GameConstants.DEFAULT_MOVE_POWER;
        else evt.secondaryBatteryCost = GameConstants.DEFAULT_MOVE_POWER;
        return new List<GameEvent>(evt);
    }
    internal virtual List<GameEvent> Attack(byte dir, bool isPrimary)
    {
        AttackEvent evt = new AttackEvent();
        evt.locs = GetVictimLocations(dir);
        evt.robotId = id;
        if (isPrimary) evt.primaryBatteryCost = GameConstants.DEFAULT_ATTACK_POWER;
        else evt.secondaryBatteryCost = GameConstants.DEFAULT_ATTACK_POWER;
        return new List<GameEvent>(evt);
    }
    internal virtual short Damage(Robot victim)
    {
        return attack;
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