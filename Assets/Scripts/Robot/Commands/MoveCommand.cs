using System.Collections.Generic;
using UnityEngine;

public class MoveCommand : RobotCommand {

    Direction direction;
    public const string LEFT = "Left";
    public const string RIGHT = "Right";
    public const string FORWARD = "Forward";
    public const string BACK = "Back";
    public static Dictionary<string, Direction> LABELTOENUM = new Dictionary<string, Direction>
    {
        { LEFT, Direction.LEFT},
        { RIGHT, Direction.RIGHT},
        { FORWARD, Direction.FORWARD},
        { BACK, Direction.BACK},
    };
    public static Dictionary<Direction, string> ENUMTOLABEL = new Dictionary<Direction, string>
    {
        { Direction.LEFT, LEFT},
        { Direction.RIGHT, RIGHT},
        { Direction.FORWARD, FORWARD},
        { Direction.BACK, BACK},
    };

    public MoveCommand(Direction dir)
    {
        direction = dir;
    }

    public enum Direction
    {
        LEFT,
        RIGHT,
        FORWARD,
        BACK
    }

    public Direction getDirection()
    {
        return direction;
    }

    public override string toString()
    {
        return RobotMenuController.MOVE + " " + ENUMTOLABEL[direction];
    }
}
