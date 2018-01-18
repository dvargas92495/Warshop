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
    internal Vector2 position;
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
        position = new Vector2(0, 0);
        orientation = Orientation.NORTH;
        inQueue = true;
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
        writer.Write(position);
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
        robot.position = reader.ReadVector2();
        robot.orientation = (Orientation)reader.ReadByte();
        robot.inQueue = reader.ReadBoolean();
        return robot;
    }
    public static Vector2 OrientationToVector (Orientation orientation)
    {
        switch (orientation)
        {
            case Orientation.NORTH:
                return Vector2.down;
            case Orientation.SOUTH:
                return Vector2.up;
            case Orientation.WEST:
                return Vector2.left;
            case Orientation.EAST:
                return Vector2.right;
            default:
                return Vector2.zero;
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
    internal List<Vector2> GetVictimLocations()
    {
        List<Vector2> locs = new List<Vector2>();
        switch (orientation)
        {
            case Orientation.NORTH:
                locs.Add(position + new Vector2(0, -1));
                break;
            case Orientation.SOUTH:
                locs.Add(position + new Vector2(0, 1));
                break;
            case Orientation.WEST:
                locs.Add(position + new Vector2(-1, 0));
                break;
            case Orientation.EAST:
                locs.Add(position + new Vector2(1, 0));
                break;
        }
        return locs;
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