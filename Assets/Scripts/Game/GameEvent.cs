using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class GameEvent {
    public bool isOpponent { set; get; }
    public BoardController board { set; get; }
    public RobotController primaryRobot { set; get; }
    public abstract void playEvent();
}

public class MoveEvent : GameEvent
{
    public MoveCommand.Direction direction { get; set; }
    public override void playEvent()
    {
        switch (direction)
        {
            case MoveCommand.Direction.BACK:
                primaryRobot.MoveBackward();
                break;
            case MoveCommand.Direction.FORWARD:
                primaryRobot.MoveForward();
                break;
            case MoveCommand.Direction.LEFT:
                primaryRobot.MoveLeft();
                break;
            case MoveCommand.Direction.RIGHT:
                primaryRobot.MoveRight();
                break;
        }
    }
}

public class AttackEvent : GameEvent
{
    public override void playEvent()
    {
        throw new NotImplementedException();
    }
}

public class RotateEvent : GameEvent
{
    public RotateCommand.Direction direction { get; set; }
    public override void playEvent()
    {
        switch (direction)
        {
            case RotateCommand.Direction.NORTH:
                primaryRobot.RotateClockwise();
                break;/*
            case RotateCommand.Direction.FORWARD:
                primaryRobot.MoveForward();
                break;
            case MoveCommand.Direction.LEFT:
                primaryRobot.MoveLeft();
                break;
            case MoveCommand.Direction.RIGHT:
                primaryRobot.MoveRight();
                break;*/
        }
    }
}

public class SpawnEvent : GameEvent
{
    public int spawnIndex { get; set; }
    public override void playEvent()
    {
        int[] tilecoords = board.GetSpawn(spawnIndex, isOpponent);
        board.PlaceRobot(primaryRobot.gameObject.transform, tilecoords[0], tilecoords[1]);
    }

    public override string ToString()
    {
        return "Spawned Robot " + primaryRobot.name + " on spawn space " + (spawnIndex+1);
    }
}
