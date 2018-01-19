using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class Game
{
    internal Player primary;
    internal Player secondary;
    Robot[] allRobots;
    internal Map board;
    internal bool gotSomeCommands;
    List<Command> commands;

    internal Game(Robot[] t, string n, Map b)
    {
        primary = new Player(t, n);
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
        commands = new List<Command>();
    }

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
            r.position = new Vector2(x, 0);
            r.orientation = Robot.Orientation.SOUTH;
            flip = !flip;
        }
        foreach (Robot r in secondary.team)
        {
            int x = flip ? board.Width - 1 : 0;
            r.position = new Vector2(x, board.Height - 1);
            r.orientation = Robot.Orientation.NORTH;
            flip = !flip;
        }
        commands = new List<Command>();
    }

    internal void Join(Robot[] t, string n)
    {
        secondary = new Player(t, n);
        allRobots = new Robot[t.Length + primary.team.Length];
        primary.team.CopyTo(allRobots, 0);
        t.CopyTo(allRobots, primary.team.Length);
        bool flip = false; //hack
        foreach (Robot r in secondary.team)
        {
            int x = flip ? board.Width - 1 : 0;
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
    public List<GameEvent> CommandsToEvents(List<Command> cmds)
    {
        commands.AddRange(cmds);
        gotSomeCommands = !gotSomeCommands;
        if (gotSomeCommands) return new List<GameEvent>();
        Dictionary<byte, HashSet<Command>> priorityToCommands = new Dictionary<byte, HashSet<Command>>();
        Dictionary<short, byte> robotIdToCurrentPriority = new Dictionary<short, byte>();
        Array.ForEach(allRobots, (Robot r) => robotIdToCurrentPriority[r.id] = r.priority);
        for (byte p = GameConstants.MAX_PRIORITY; p > 0; p--)
        {
            priorityToCommands[p] = new HashSet<Command>();
        }
        commands.ForEach((Command c) =>
        {
            short robotId = c.robotId;
            priorityToCommands[robotIdToCurrentPriority[robotId]].Add(c);
            robotIdToCurrentPriority[robotId]--;
        });
        List<GameEvent> events = new List<GameEvent>();
        for (byte p = GameConstants.MAX_PRIORITY; p > 0; p--)
        {
            HashSet<Command> currentCmds = priorityToCommands[p];
            List<GameEvent> priorityEvents = new List<GameEvent>();

            HashSet<Command> rotateCmds = new HashSet<Command>(currentCmds.Where((Command c) => c is Command.Rotate));
            IEnumerable<GameEvent> rotateEvents = rotateCmds.ToList().ConvertAll(((Command c) => {
                Robot primaryRobot = Array.Find(allRobots, (Robot r) => r.id == c.robotId);
                return new GameEvent.Rotate(primaryRobot, ((Command.Rotate)c).direction);
            }));
            priorityEvents.AddRange(rotateEvents);

            HashSet<Command> movCmds = new HashSet<Command>(currentCmds.Where((Command c) => c is Command.Move));
            IEnumerable<GameEvent> movEvents = movCmds.ToList().ConvertAll(((Command c) => {
                Robot primaryRobot = Array.Find(allRobots, (Robot r) => r.id == c.robotId);
                return new GameEvent.Move(primaryRobot, ((Command.Move)c).direction);
            }));
            priorityEvents.AddRange(movEvents);

            HashSet<Command> attackCmds = new HashSet<Command>(currentCmds.Where((Command c) => c is Command.Attack));
            IEnumerable<GameEvent> attackEvents = attackCmds.ToList().ConvertAll(((Command c) => {
                Robot primaryRobot = Array.Find(allRobots, (Robot r) => r.id == c.robotId);
                List<Vector2> locs = primaryRobot.GetVictimLocations();
                Robot[] victims = Array.FindAll(allRobots, (robot) => locs.Contains(robot.position));
                return new GameEvent.Attack(primaryRobot, victims);
            }));
            priorityEvents.AddRange(attackEvents);

            HashSet<Command> specialCmds = new HashSet<Command>(currentCmds.Where((Command c) => c is Command.Special));
            IEnumerable<GameEvent> specialEvents = specialCmds.ToList().ConvertAll(((Command c) => {
                return new GameEvent.Empty();
            }));
            priorityEvents.AddRange(specialEvents);

            priorityEvents.ForEach((GameEvent e) => e.priority = p);
            events.AddRange(priorityEvents);
        }
        commands.Clear();
        return events;
    }
}
