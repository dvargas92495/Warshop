using System;
using System.Collections.Generic;
using UnityEngine;

internal class Robot
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
    internal enum Orientation
    {
        NORTH,
        SOUTH,
        WEST,
        EAST
    }
    internal enum Rating
    {
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