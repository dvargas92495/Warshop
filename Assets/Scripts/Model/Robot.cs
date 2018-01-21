using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Robot
{
    internal readonly string name;
    internal readonly string description;
    internal byte priority;
    internal short health;
    internal short attack;
    internal Rating rating;
    internal short id;
    internal Vector2Int position;
    internal Orientation orientation;
    internal bool inQueue;
    private Robot(string _name, string _description)
    {
        name = _name;
        description = _description;
    }
    internal Robot(string _name, string _description, byte _priority, short _health, short _attack, Rating _rating)
    {
        name = _name;
        description = _description;
        priority = _priority;
        health = _health;
        attack = _attack;
        rating = _rating;
        position = Vector2Int.zero;
        orientation = Orientation.NORTH;
        inQueue = true;
    }
    public static Robot Get(Robot[] allRobots, short id)
    {
        return Array.Find(allRobots, (Robot r) => r.id == id);
    }
    internal static Robot create(string robotName)
    {
        switch(robotName)
        {
            case Slinkbot._name:
                return new Slinkbot();
            case Pithon._name:
                return new Pithon();
            case Virusbot._name:
                return new Virusbot();
            case Jaguar._name:
                return new Jaguar();
            case Flybot._name:
                return new Flybot();
            default:
                throw new Exception("Invalid Robot Name: " + robotName);
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
        writer.Write((byte)orientation);
        writer.Write(inQueue);
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
        robot.orientation = (Orientation)reader.ReadByte();
        robot.inQueue = reader.ReadBoolean();
        return robot;
    }
    public static Vector2Int OrientationToVector (Orientation orientation)
    {
        switch (orientation)
        {
            case Orientation.NORTH:
                return Vector2Int.down;
            case Orientation.SOUTH:
                return Vector2Int.up;
            case Orientation.WEST:
                return Vector2Int.left;
            case Orientation.EAST:
                return Vector2Int.right;
            default:
                return Vector2Int.zero;
        }
    }
    public enum Orientation
    {
        NORTH,
        SOUTH,
        WEST,
        EAST
    }
    internal enum Rating
    {
        PLATINUM = 4,
        GOLD = 3,
        SILVER = 2,
        BRONZE = 1
    }
    internal List<GameEvent> Rotate(Command.Direction dir, bool isPrimary)
    {
        GameEvent.Rotate evt = new GameEvent.Rotate();
        evt.sourceDir = orientation;
        switch (dir)
        {
            case Command.Direction.UP:
                orientation = (isPrimary ? Orientation.SOUTH : Orientation.NORTH);
                break;
            case Command.Direction.DOWN:
                orientation = (isPrimary ? Orientation.NORTH : Orientation.SOUTH);
                break;
            case Command.Direction.LEFT:
                orientation = (isPrimary ? Orientation.EAST : Orientation.WEST);
                break;
            case Command.Direction.RIGHT:
                orientation = (isPrimary ? Orientation.WEST : Orientation.EAST);
                break;
        }
        evt.destinationDir = orientation;
        evt.primaryRobotId = id;
        evt.primaryBattery = (isPrimary ? GameConstants.DEFAULT_ROTATE_POWER : (short)0);
        evt.secondaryBattery = (isPrimary ? (short)0 : GameConstants.DEFAULT_ROTATE_POWER);
        return new List<GameEvent>() { evt };
    }
    internal List<GameEvent> Move(Vector2Int diff, bool isPrimary)
    {
        GameEvent.Move evt = new GameEvent.Move();
        evt.sourcePos = position;
        position += diff;
        evt.destinationPos = position;
        evt.primaryRobotId = id;
        evt.primaryBattery = (isPrimary ? GameConstants.DEFAULT_MOVE_POWER : (short)0);
        evt.secondaryBattery = (isPrimary ? (short)0 : GameConstants.DEFAULT_MOVE_POWER);
        return new List<GameEvent>() { evt };
    }
    internal List<GameEvent> Push(Vector2Int diff, Robot victim, bool isPrimary)
    {
        GameEvent.Push evt = new GameEvent.Push();
        evt.sourcePos = position;
        position += diff;
        evt.transferPos = position;
        victim.position += diff;
        evt.destinationPos = victim.position;
        evt.victim = victim.id;
        evt.primaryRobotId = id;
        evt.primaryBattery = (isPrimary ? GameConstants.DEFAULT_MOVE_POWER : (short)0);
        evt.secondaryBattery = (isPrimary ? (short)0 : GameConstants.DEFAULT_MOVE_POWER);
        return new List<GameEvent>() { evt };
    }
    internal List<Vector2Int> GetVictimLocations()
    {
        return new List<Vector2Int>() { position + OrientationToVector(orientation) };
    }
    internal List<GameEvent> Attack(Robot[] victims, bool isPrimary)
    {
        GameEvent.Attack evt = new GameEvent.Attack();
        evt.victimIds = new short[victims.Length];
        evt.victimHealth = new short[victims.Length];
        for (int i = 0; i < victims.Length; i++)
        {
            Robot victim = victims[i];
            evt.victimIds[i] = victim.id;
            victim.health -= attack;
            evt.victimHealth[i] = victim.health;
        }
        evt.primaryRobotId = id;
        evt.primaryBattery = (isPrimary ? GameConstants.DEFAULT_ATTACK_POWER : (short)0);
        evt.secondaryBattery = (isPrimary ? (short)0 : GameConstants.DEFAULT_ATTACK_POWER);
        return new List<GameEvent>() { evt };
    }

    internal IEnumerable<GameEvent> Miss(bool isPrimary)
    {
        GameEvent.Miss evt = new GameEvent.Miss();
        evt.primaryRobotId = id;
        evt.primaryBattery = (isPrimary ? GameConstants.DEFAULT_ATTACK_POWER : (short)0);
        evt.secondaryBattery = (isPrimary ? (short)0 : GameConstants.DEFAULT_ATTACK_POWER);
        return new List<GameEvent>() { evt };
    }
    internal List<GameEvent> Battery(bool opponentsBase, bool isPrimary)
    {
        GameEvent.Battery evt = new GameEvent.Battery();
        evt.opponentsBase = opponentsBase;
        evt.damage = attack;
        evt.primaryRobotId = id;
        evt.primaryBattery = (isPrimary ? GameConstants.DEFAULT_ATTACK_POWER : (short)0);
        evt.secondaryBattery = (isPrimary ? (short)0 : GameConstants.DEFAULT_ATTACK_POWER);
        short drain = (short)(GameConstants.DEFAULT_BATTERY_MULTIPLIER * attack);
        if ((opponentsBase && !isPrimary) || (!opponentsBase && isPrimary))
        {
            evt.primaryBattery += drain;
        } else
        {
            evt.secondaryBattery += drain;
        }
        return new List<GameEvent>() { evt };
    }
    private class Slinkbot : Robot
    {
        internal const string _name = "Slinkbot";
        internal const string _description = "Forward Moves are 2 Spaces";
        internal Slinkbot() : base(
            _name,
            _description,
            5, 6, 1,
            Rating.BRONZE
        )
        {}
    }

    private class Pithon : Robot
    {
        internal const string _name = "Pithon";
        internal const string _description = "Poison";
        internal Pithon() : base(
            _name,
            _description,
            6, 5, 2,
            Rating.SILVER
        )
        { }
    }

    private class Virusbot : Robot
    {
        internal const string _name = "Virusbot";
        internal const string _description = "Enemies damaged by this bot cannot move next turn";
        internal Virusbot() : base(
            _name,
            _description,
            7, 4, 1,
            Rating.SILVER
        )
        { }
    }

    private class Jaguar : Robot
    {
        internal const string _name = "Jaguar";
        internal const string _description = "Can Move a Third Time (-1 Attack)";
        internal Jaguar() : base(
            _name,
            _description,
            8, 4, 1,
            Rating.SILVER
        )
        { }
    }

    private class Flybot : Robot
    {
        internal const string _name = "Flybot";
        internal const string _description = "Can't be damaged, poisoned";
        internal Flybot() : base(
            _name,
            _description,
            6, 6, 2,
            Rating.SILVER
        )
        { }
    }
}