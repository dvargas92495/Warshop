﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class Game
{
    internal Player primary = new Player();
    internal Player secondary = new Player();
    Robot[] allRobots = new Robot[0];
    internal Map board;
    internal short nextRobotId;

    internal Game() { }

    internal void Join(string[] t, string n, int cid)
    {
        bool isPrimary = !primary.ready;
        Robot[] robots = new Robot[t.Length];
        for (byte i = 0; i < t.Length; i++)
        {
            Robot r = Robot.create(t[i]);
            r.id = nextRobotId;
            nextRobotId++;
            r.queueSpot = i;
            r.position = board.GetQueuePosition(r.queueSpot, isPrimary);
            board.UpdateObjectLocation(r.position.x, r.position.y, r.id);
            r.orientation = isPrimary ? Robot.Orientation.SOUTH : Robot.Orientation.NORTH;
            robots[i] = r;
        }
        allRobots = new Robot[allRobots.Length + robots.Length];
        robots.CopyTo(allRobots, 0);
        if (!isPrimary)
        {
            primary.team.CopyTo(allRobots, robots.Length);
            secondary = new Player(robots, n);
            secondary.connectionId = cid;
        } else
        {
            primary = new Player(robots, n);
            primary.connectionId = cid;
        }
    }

    internal HashSet<int> connectionIds()
    {
        HashSet<int> ids = new HashSet<int>();
        ids.Add(primary.connectionId);
        ids.Add(secondary.connectionId);
        return ids;
    }

    internal Messages.GameReadyMessage GetGameReadyMessage(bool forPrimary)
    {
        primary.ready = secondary.ready = false;
        Messages.GameReadyMessage resp = new Messages.GameReadyMessage();
        resp.board = board;
        resp.myTeam = (forPrimary ? primary.team : secondary.team);
        resp.opponentTeam = (forPrimary ? secondary.team : primary.team);
        resp.opponentname = (forPrimary ? secondary.name : primary.name);
        return resp;
    }

    public class Player
    {
        internal string name;
        internal short battery = GameConstants.POINTS_TO_WIN;
        internal Robot[] team;
        internal bool ready;
        internal List<Command> commands;
        internal int connectionId;
        internal Player()
        {
            ready = false;
        }
        internal Player(Robot[] t, string n)
        {
            team = t;
            name = n;
            ready = true;
        }
        internal void StoreCommands(List<Command> cmds)
        {
            commands = cmds;
            ready = true;
        }
        internal List<Command> FetchCommands()
        {
            List<Command> cmds = new List<Command>(commands);
            ready = false;
            commands.Clear();
            return cmds;
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

    public List<GameEvent> CommandsToEvents()
    {
        List<Command> commands = new List<Command>();
        commands.AddRange(primary.FetchCommands());
        commands.AddRange(secondary.FetchCommands());
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

            priorityEvents.AddRange(processFinish(primary.team, true));
            priorityEvents.AddRange(processFinish(secondary.team, false));

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
                        events.Add(evt);
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

            Vector2Int newspace = primaryRobot.position + diff;
            Action<string> generateBlockEvent = (string s) =>
            {
                GameEvent.Block evt = new GameEvent.Block();
                evt.primaryRobotId = c.robotId;
                evt.blockingObject = s;
                evt.primaryBattery = (c.owner.Equals(primary.name) ? GameConstants.DEFAULT_MOVE_POWER : (short)0);
                evt.secondaryBattery = (c.owner.Equals(primary.name) ? (short)0 : GameConstants.DEFAULT_MOVE_POWER);
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
            if (space.spaceType == Map.Space.SpaceType.PRIMARY_QUEUE && space.spaceType == Map.Space.SpaceType.SECONDARY_QUEUE)
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

    private List<GameEvent> processFinish(Robot[] team, bool isPrimary)
    {
        List<GameEvent> evts = new List<GameEvent>();
        Array.ForEach(team, (Robot r) =>
        {
            if (r.health <= 0)
            {
                Vector2Int v = board.GetQueuePosition(r.queueSpot, isPrimary);
                evts.AddRange(r.Death(v, isPrimary));
                board.UpdateObjectLocation(r.position.x, r.position.y, r.id);
            }
        });
        return evts;
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
