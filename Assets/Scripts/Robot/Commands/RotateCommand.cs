using System;
using System.Collections.Generic;
using UnityEngine;

public class RotateCommand : RobotCommand
{

    Direction direction;
    public const string NORTH = "North";
    public const string SOUTH = "South";
    public const string EAST = "East";
    public const string WEST = "West";
    public static Dictionary<string, Direction> LABELTOENUM = new Dictionary<string, Direction>
    {
        { NORTH, Direction.NORTH},
        { SOUTH, Direction.SOUTH},
        { WEST, Direction.WEST},
        { EAST, Direction.EAST }
    };
    public static Dictionary<Direction, string> ENUMTOLABEL = new Dictionary<Direction, string>
    {
        { Direction.NORTH, NORTH},
        { Direction.SOUTH, SOUTH},
        { Direction.WEST, WEST},
        { Direction.EAST, EAST }
    };

    public RotateCommand(Direction dir)
    {
        direction = dir;
    }

    public enum Direction
    {
        NORTH,
        SOUTH,
        EAST,
        WEST
    }

    public Direction getDirection()
    {
        return direction;
    }

    public override string toString()
    {
        return RobotMenuController.ROTATE + " " + ENUMTOLABEL[direction];
    }
}
