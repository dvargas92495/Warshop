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
            r.position = new Vector2Int(x, 0);
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
            r.position = new Vector2Int(x, 0);
            board.UpdateObjectLocation(x, 0, r.id);
            r.orientation = Robot.Orientation.SOUTH;
            flip = !flip;
        }
        foreach (Robot r in secondary.team)
        {
            int x = flip ? board.Width - 1 : 0;
            r.position = new Vector2Int(x, board.Height - 1);
            board.UpdateObjectLocation(x, board.Height - 1, r.id);
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
            r.position = new Vector2Int(x, board.Height - 1);
            board.UpdateObjectLocation(x, board.Height - 1, r.id);
            r.orientation = Robot.Orientation.NORTH;
            flip = !flip;
        }
    }

    public class Player
    {
        internal string name;
        internal short battery;
        internal Robot[] team;
        internal Player(Robot[] t, string n)
        {
            team = t;
            name = n;
            battery = GameConstants.POINTS_TO_WIN;
        }
    }

    private class RobotTurnObject
    {
        internal byte numRotates = 0;
        internal byte numMoves = 0;
        internal byte numAttacks;
        internal byte numSpecials;
        internal byte priority;
        internal RobotTurnObject(byte p)
        {
            priority = p;
        }
    }

    public List<GameEvent> CommandsToEvents(List<Command> cmds)
    {
        commands.AddRange(cmds);
        gotSomeCommands = !gotSomeCommands;
        if (gotSomeCommands) return new List<GameEvent>();
        Dictionary<byte, HashSet<Command>> priorityToCommands = new Dictionary<byte, HashSet<Command>>();
        Dictionary<short, RobotTurnObject> robotIdToTurnObject = new Dictionary<short, RobotTurnObject>();
        Array.ForEach(allRobots, (Robot r) => robotIdToTurnObject[r.id] = new RobotTurnObject(r.priority));
        for (byte p = GameConstants.MAX_PRIORITY; p > 0; p--)
        {
            priorityToCommands[p] = new HashSet<Command>();
        }
        commands.ForEach((Command c) =>
        {
            short robotId = c.robotId;
            priorityToCommands[robotIdToTurnObject[robotId].priority].Add(c);
            robotIdToTurnObject[robotId].priority--;
        });
        List<GameEvent> events = new List<GameEvent>();
        for (byte p = GameConstants.MAX_PRIORITY; p > 0; p--)
        {
            HashSet<Command> currentCmds = priorityToCommands[p];
            List<GameEvent> priorityEvents = new List<GameEvent>();

            HashSet<Command.Rotate> rotateCmds = new HashSet<Command.Rotate>(currentCmds.Where((Command c) => c is Command.Rotate).Select((Command c) => (Command.Rotate)c));
            rotateCmds.ToList().ForEach((Command.Rotate c) =>
            {
                RobotTurnObject rto = robotIdToTurnObject[c.robotId];
                if (rto.numRotates < GameConstants.DEFAULT_ROTATE_LIMIT)
                {
                    rto.numRotates++;
                } else
                {
                    GameEvent.Fail fail = new GameEvent.Fail();
                    fail.failedCmd = "Rotate";
                    fail.primaryRobotId = c.robotId;
                    priorityEvents.Add(fail);
                    rotateCmds.Remove(c);
                }
            });
            IEnumerable<GameEvent> rotateEvents = processRotateCommands(rotateCmds);
            priorityEvents.AddRange(rotateEvents);

            HashSet<Command.Move> movCmds = new HashSet<Command.Move>(currentCmds.Where((Command c) => c is Command.Move).Select((Command c) => (Command.Move)c));
            movCmds.ToList().ForEach((Command.Move c) =>
            {
                RobotTurnObject rto = robotIdToTurnObject[c.robotId];
                if (rto.numMoves < GameConstants.DEFAULT_MOVE_LIMIT)
                {
                    rto.numMoves++;
                }
                else
                {
                    GameEvent.Fail fail = new GameEvent.Fail();
                    fail.failedCmd = "Move";
                    fail.primaryRobotId = c.robotId;
                    priorityEvents.Add(fail);
                    movCmds.Remove(c);
                }
            });
            IEnumerable<GameEvent> movEvents = processMoveCommands(movCmds);
            priorityEvents.AddRange(movEvents);

            HashSet<Command.Attack> attackCmds = new HashSet<Command.Attack>(currentCmds.Where((Command c) => c is Command.Attack).Select((Command c) => (Command.Attack)c));
            attackCmds.ToList().ForEach((Command.Attack c) =>
            {
                RobotTurnObject rto = robotIdToTurnObject[c.robotId];
                if (rto.numAttacks < GameConstants.DEFAULT_ATTACK_LIMIT)
                {
                    rto.numAttacks++;
                }
                else
                {
                    GameEvent.Fail fail = new GameEvent.Fail();
                    fail.failedCmd = "Attack";
                    fail.primaryRobotId = c.robotId;
                    priorityEvents.Add(fail);
                    attackCmds.Remove(c);
                }
            });
            IEnumerable<GameEvent> attackEvents = processAttackCommands(attackCmds);
            priorityEvents.AddRange(attackEvents);

            HashSet<Command.Special> specialCmds = new HashSet<Command.Special>(currentCmds.Where((Command c) => c is Command.Special).Select((Command c) => (Command.Special)c));
            specialCmds.ToList().ForEach((Command.Special c) =>
            {
                RobotTurnObject rto = robotIdToTurnObject[c.robotId];
                if (rto.numSpecials < GameConstants.DEFAULT_SPECIAL_LIMIT)
                {
                    rto.numSpecials++;
                }
                else
                {
                    GameEvent.Fail fail = new GameEvent.Fail();
                    fail.failedCmd = "Special";
                    fail.primaryRobotId = c.robotId;
                    priorityEvents.Add(fail);
                    specialCmds.Remove(c);
                }
            });
            IEnumerable<GameEvent> specialEvents = specialCmds.ToList().ConvertAll(((Command.Special c) => {
                return new GameEvent.Empty();
            }));
            priorityEvents.AddRange(specialEvents);

            processBatteryLoss(priorityEvents, p);
            events.AddRange(priorityEvents);
        }
        commands.Clear();
        return events;
    }

    private List<GameEvent> processRotateCommands(HashSet<Command.Rotate> rotateCmds)
    {
        List<GameEvent> events = new List<GameEvent>();
        rotateCmds.ToList().ForEach((Command.Rotate c) => {
            List<GameEvent> evts = Robot.Get(allRobots, c.robotId).Rotate(c.direction, c.owner.Equals(primary.name));
            events.AddRange(evts); 
        });
        return events;
    }

    private List<GameEvent> processMoveCommands(HashSet<Command.Move> moves)
    {
        List<GameEvent> events = new List<GameEvent>();
        Dictionary<short, Vector2Int> idsToDiffs = new Dictionary<short, Vector2Int>();
        moves.ToList().ForEach((Command.Move c) =>
        {
            Vector2Int diff = Vector2Int.zero;
            switch (c.direction)
            {
                case Command.Direction.UP:
                    diff = Vector2Int.down;
                    break;
                case Command.Direction.DOWN:
                    diff = Vector2Int.up;
                    break;
                case Command.Direction.LEFT:
                    diff = Vector2Int.left;
                    break;
                case Command.Direction.RIGHT:
                    diff = Vector2Int.right;
                    break;
            }
            if (c.owner.Equals(primary.name))
            {
                diff = new Vector2Int(-diff.x, -diff.y);
            }
            idsToDiffs[c.robotId] = diff;
        });
        Array.ForEach(board.spaces, (Map.Space s) => {
            List<short> idsWantSpace = idsToDiffs.Keys.ToList().FindAll((short key) => {
                Robot primaryRobot = Robot.Get(allRobots, key);
                Vector2 newspace = primaryRobot.position + idsToDiffs[key];
                return newspace.x == s.x && newspace.y == s.y;
            });
            if (idsWantSpace.Count > 1)
            {
                List<short> facing = idsWantSpace.FindAll((short key) => {
                    Robot primaryRobot = Robot.Get(allRobots, key);
                    return idsToDiffs[key].Equals(Robot.OrientationToVector(primaryRobot.orientation));
                });
                string blocker = "";
                if (facing.Count == 1)
                {
                    Robot winner = Robot.Get(allRobots, facing[0]);
                    idsWantSpace.Remove(winner.id);
                    blocker = winner.name;
                }
                else
                {
                    blocker = "Each Other";
                }
                if (board.IsSpaceOccupied(s))
                {
                    blocker = Robot.Get(allRobots, board.GetIdOnSpace(s)).name;
                }
                moves.RemoveWhere((Command.Move c) => {
                    if (idsWantSpace.Contains(c.robotId))
                    {
                        GameEvent.Block evt = new GameEvent.Block();
                        evt.primaryRobotId = c.robotId;
                        evt.blockingObject = blocker;
                        evt.primaryBattery = (c.owner.Equals(primary.name) ? GameConstants.DEFAULT_MOVE_POWER : (short)0);
                        evt.secondaryBattery = (c.owner.Equals(primary.name) ? (short)0:GameConstants.DEFAULT_MOVE_POWER);
                        return true;
                    }
                    return false;
                });
            }
        });
        HashSet<Robot> robotLocationsToUpdate = new HashSet<Robot>();
        moves.ToList().ForEach((Command.Move c) =>
        {
            Robot primaryRobot = Robot.Get(allRobots, c.robotId);
            Vector2Int diff = idsToDiffs[c.robotId];

            Vector2 newspace = primaryRobot.position + diff;
            Action<string> generateBlockEvent = (string s) =>
            {
                GameEvent.Block evt = new GameEvent.Block();
                evt.primaryRobotId = c.robotId;
                evt.blockingObject = s;
                evt.primaryBattery = (c.owner.Equals(primary.name) ? GameConstants.DEFAULT_MOVE_POWER : (short)0);
                evt.secondaryBattery = (c.owner.Equals(secondary.name) ? (short)0 : GameConstants.DEFAULT_MOVE_POWER);
                events.Add(evt);
                moves.Remove(c);
            };
            if (newspace.x < 0 || newspace.x >= board.Width || newspace.y < 0 || newspace.y >= board.Height)
            {
                generateBlockEvent("Wall");
                return;
            }
            Map.Space space = board.spaces[(int)newspace.y * board.Width + (int)newspace.x];
            if (space.spaceType == Map.Space.SpaceType.PRIMARY_BASE || space.spaceType == Map.Space.SpaceType.SECONDARY_BASE)
            {
                generateBlockEvent("Base");
                return;
            }
            if (space.spaceType == Map.Space.SpaceType.QUEUE)
            {
                generateBlockEvent("Queue");
                return;
            }
            if (board.IsSpaceOccupied(space))
            {
                Robot currentBot = Robot.Get(allRobots, board.GetIdOnSpace(space));
                if (!Robot.OrientationToVector(primaryRobot.orientation).Equals(diff) || // Not Facing Direction
                Robot.OrientationToVector(currentBot.orientation).Equals(primaryRobot.position - newspace)) //Or currentBot facing it
                {
                    generateBlockEvent(currentBot.name);
                }
                else
                {
                    events.AddRange(primaryRobot.Push(diff, currentBot, c.owner.Equals(primary.name)));
                    robotLocationsToUpdate.Add(currentBot);
                    robotLocationsToUpdate.Add(primaryRobot);
                }
            }
            else
            {
                events.AddRange(primaryRobot.Move(diff, c.owner.Equals(primary.name)));
                robotLocationsToUpdate.Add(primaryRobot);
            }
        });

        robotLocationsToUpdate.ToList().ForEach(((Robot r) => {
            board.UpdateObjectLocation((int)r.position.x, (int)r.position.y, r.id);
        }));

        return events;
    }

    private List<GameEvent> processAttackCommands(HashSet<Command.Attack> attackCmds)
    {
        List<GameEvent> events = new List<GameEvent>();
        attackCmds.ToList().ForEach(((Command.Attack c) => {
            Robot primaryRobot = Robot.Get(allRobots, c.robotId);
            List<Vector2Int> locs = primaryRobot.GetVictimLocations();
            List<GameEvent> evts = new List<GameEvent>();
            locs.ForEach((Vector2Int v) =>
            {
                bool isPrimary = c.owner.Equals(primary.name);
                if (board.getSpaceType(v.x,v.y) == Map.Space.SpaceType.PRIMARY_BASE)
                {
                    evts.AddRange(primaryRobot.Battery(!isPrimary, isPrimary));
                }
                else if(board.getSpaceType(v.x, v.y) == Map.Space.SpaceType.SECONDARY_BASE)
                {
                    evts.AddRange(primaryRobot.Battery(isPrimary, isPrimary));
                }
            });
            Robot[] victims = Array.FindAll(allRobots, (robot) => locs.Contains(robot.position));
            if (victims.Length == 0 && evts.Count == 0)
            {
                evts.AddRange(primaryRobot.Miss(c.owner.Equals(primary.name)));
            }
            else if (victims.Length > 0)
            {
                evts.AddRange(primaryRobot.Attack(victims, c.owner.Equals(primary.name)));
            }
            events.AddRange(evts);
        }));
        return events;
    }

    private List<GameEvent> processSpecialCommands(HashSet<Command.Special> rotateCmds)
    {
        return new List<GameEvent>();
    }

    private void processBatteryLoss(List<GameEvent> evts, byte p)
    {
        evts.ForEach((GameEvent e) =>
        {
            e.priority = p;
            primary.battery -= e.primaryBattery;
            e.primaryBattery = primary.battery;
            secondary.battery -= e.secondaryBattery;
            e.secondaryBattery = secondary.battery;
        });
    }
}
