using System;
using System.Collections.Generic;
using UnityEngine;

public class Game
{
    Player primary;
    Player secondary;
    Robot[] allRobots;
    Map board;

    internal Game(Robot[] t1, Robot[] t2, string n1, string n2, Map b)
    {
        primary = new Player(t1, n1);
        secondary = new Player(t2, n2);
        allRobots = new Robot[t1.Length + t2.Length];
        t1.CopyTo(allRobots, 0);
        t2.CopyTo(allRobots, t1.Length);
        board = b;
        bool flip = false; //hack
        foreach (Robot r in primary.team)
        {
            int x = flip ? board.Width - 1 : 0;
            b.UpdateObjectLocation(x, 0, r.id);
            r.position = new Vector2(x, 0);
            r.orientation = Robot.Orientation.SOUTH;
            flip = !flip;
        }
        foreach (Robot r in secondary.team)
        {
            int x = flip ? board.Width - 1 : 0;
            b.UpdateObjectLocation(x, board.Height - 1, r.id);
            r.position = new Vector2(x, board.Height - 1);
            r.orientation = Robot.Orientation.NORTH;
            flip = !flip;
        }
    }

    public class Player
    {
        internal string name;
        internal int battery;
        internal Robot[] team;
        internal Player(Robot[] t, string n)
        {
            team = t;
            name = n;
            battery = GameConstants.POINTS_TO_WIN;
        }
    }

    //TODO: Move this to Server
    public List<GameEvent> CommandsToEvents(List<Command> commands)
    {
        commands.Sort((a, b) =>
        {
            return -1;
        });
        List<GameEvent> events = new List<GameEvent>();
        foreach (Command cmd in commands)
        {
            GameEvent e = null;
            Robot primaryRobot = Array.Find(allRobots, (Robot r) => r.id == cmd.robotId);
            if (cmd is Command.Move)
            {
                e = new GameEvent.Move(primaryRobot, ((Command.Move)cmd).direction);
            }
            else if (cmd is Command.Rotate)
            {
                e = new GameEvent.Rotate(primaryRobot, ((Command.Rotate)cmd).direction);
            }
            else if (cmd is Command.Attack)
            {
                List<Vector2> locs = primaryRobot.GetVictimLocations();
                Robot[] victims = Array.FindAll(allRobots, (robot) => locs.Contains(robot.position));
                e = new GameEvent.Attack(primaryRobot, victims);
            }
            else
            {
                Logger.ServerLog("Bad Command: " + cmd.ToString());
                continue;
            }
            events.Add(e);
        }
        return events;
    }
}
