using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class Game
{
    internal Player primary = new Player();
    internal Player secondary = new Player();
    Robot[] allRobots = new Robot[0];
    internal List<GameEvent> endOfTurnEvents = new List<GameEvent>();
    internal Map board;
    internal short nextRobotId;
    internal int turn = 1;

    internal Game() {}

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
            r.orientation = isPrimary ? Robot.Orientation.NORTH : Robot.Orientation.SOUTH;
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

    internal List<int> connectionIds()
    {
        List<int> ids = new List<int>();
        ids.Add(primary.connectionId);
        if (!ids.Contains(secondary.connectionId)) ids.Add(secondary.connectionId);
        return ids;
    }

    internal Messages.GameReadyMessage GetGameReadyMessage(bool forPrimary)
    {
        primary.ready = secondary.ready = false;
        Messages.GameReadyMessage resp = new Messages.GameReadyMessage();
        resp.isPrimary = forPrimary;
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
            commands.ForEach((Command c) => c.owner = name);
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

    internal class RobotTurnObject
    {
        internal Dictionary<Type, byte> num = new Dictionary<Type, byte>()
        {
            { typeof(Command.Rotate), 0 },
            { typeof(Command.Move), 0 },
            { typeof(Command.Attack), 0 },
            { typeof(Command.Special), 0 }
        };
        static internal Dictionary<Type, byte> limit = new Dictionary<Type, byte>()
        {
            { typeof(Command.Rotate), GameConstants.DEFAULT_ROTATE_LIMIT },
            { typeof(Command.Move), GameConstants.DEFAULT_MOVE_LIMIT },
            { typeof(Command.Attack), GameConstants.DEFAULT_ATTACK_LIMIT },
            { typeof(Command.Special), GameConstants.DEFAULT_SPECIAL_LIMIT }
        };
        /*static internal Dictionary<string, byte> power = new Dictionary<string, byte>()
        {
            { typeof(Command.Rotate).ToString(), GameConstants.DEFAULT_ROTATE_POWER },
            { typeof(Command.Move).ToString(), GameConstants.DEFAULT_MOVE_POWER },
            { typeof(Command.Attack).ToString(), GameConstants.DEFAULT_ATTACK_POWER },
            { typeof(Command.Special).ToString(), GameConstants.DEFAULT_SPECIAL_POWER }
        };*/ // Uncomment if we want fails to cost power.
        internal byte priority;
        internal RobotTurnObject(byte p)
        {
            priority = p;
        }
    }

    public int GetTurn()
    {
        int turnNumber = turn;
        turn++;
        return turnNumber;
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
            if (robotIdToTurnObject[c.robotId].priority > 0)
            {
                priorityToCommands[robotIdToTurnObject[c.robotId].priority].Add(c);
                robotIdToTurnObject[c.robotId].priority--;
            }
        });
        List<GameEvent> events = new List<GameEvent>();
        for (byte p = GameConstants.MAX_PRIORITY; p > 0; p--)
        {
            HashSet<Command> currentCmds = priorityToCommands[p];
            List<GameEvent> priorityEvents = new List<GameEvent>();

            priorityEvents.AddRange(processCommands(currentCmds, robotIdToTurnObject, typeof(Command.Rotate)));
            priorityEvents.AddRange(processCommands(currentCmds, robotIdToTurnObject, typeof(Command.Move)));
            priorityEvents.AddRange(processCommands(currentCmds, robotIdToTurnObject, typeof(Command.Attack)));
            priorityEvents.AddRange(processCommands(currentCmds, robotIdToTurnObject, typeof(Command.Special)));

            processBatteryLoss(priorityEvents, p);
            events.AddRange(priorityEvents);
        }
        List<GameEvent> priorityZeroEvents = processEndOfTurn();
        priorityZeroEvents.AddRange(processPriorityFinish(primary.team, true));
        priorityZeroEvents.AddRange(processPriorityFinish(secondary.team, false));
        processBatteryLoss(priorityZeroEvents, 0);
        events.AddRange(priorityZeroEvents);
        return events;
    }

    private List<GameEvent> processCommands(HashSet<Command> allCommands, Dictionary<short, RobotTurnObject> robotIdToTurnObject, Type t)
    {
        List<GameEvent> events = new List<GameEvent>();
        HashSet<Command> commands = new HashSet<Command>(allCommands.Where((Command c) => c.GetType().Equals(t)));
        commands.ToList().ForEach((Command c) =>
        {
            Robot primaryRobot = GetRobot(c.robotId);
            List<GameEvent> evts = primaryRobot.CheckFail(c, robotIdToTurnObject[c.robotId]);
            if (evts.Count > 0)
            {
                events.AddRange(evts);
                commands.Remove(c);
            }
        });

        Dictionary<short, List<GameEvent>> idsToWantedEvents = new Dictionary<short, List<GameEvent>>();
        commands.ToList().ForEach((Command c) =>
        {
            Robot primaryRobot = GetRobot(c.robotId);
            bool isPrimary = c.owner.Equals(primary.name);
            if (c is Command.Rotate)
            {
                idsToWantedEvents[c.robotId] = primaryRobot.Rotate(((Command.Rotate)c).direction, isPrimary);
            } else if (c is Command.Move)
            {
                board.RemoveObjectLocation(c.robotId);
                idsToWantedEvents[c.robotId] = primaryRobot.Move(((Command.Move)c).direction, isPrimary);
            } else if (c is Command.Attack)
            {
                if (board.GetQueuePosition(primaryRobot.queueSpot, isPrimary).Equals(primaryRobot.position)) events.Add(primaryRobot.Fail(c));
                else idsToWantedEvents[c.robotId] = primaryRobot.Attack(isPrimary);
            }
        });

        bool valid = false;
        while (!valid)
        {
            valid = true;
            idsToWantedEvents.Keys.ToList().ForEach((short id) =>
            {
                valid = Validate(idsToWantedEvents[id]);
            });
            if (!valid) continue;
            valid = AreValidTogether(idsToWantedEvents);
        }
        idsToWantedEvents.Values.ToList().ForEach(events.AddRange);
        events.ForEach(Update);
        events.AddRange(processPriorityFinish(primary.team, true));
        events.AddRange(processPriorityFinish(secondary.team, false));
        return events;
    }

    private List<GameEvent> processPriorityFinish(Robot[] team, bool isPrimary)
    {
        List<GameEvent> evts = new List<GameEvent>();
        Array.ForEach(team, (Robot r) =>
        {
            if (r.health <= 0)
            {
                Vector2Int v = board.GetQueuePosition(r.queueSpot, isPrimary);
                GameEvent.Death death = new GameEvent.Death();
                r.health = death.returnHealth = r.startingHealth;
                r.position = death.returnLocation = v;
                r.orientation = death.returnDir = isPrimary ? Robot.Orientation.NORTH : Robot.Orientation.SOUTH;
                death.primaryRobotId = r.id;
                death.primaryBattery = (short)(isPrimary ? GameConstants.DEFAULT_DEATH_MULTIPLIER * (byte)r.rating : 0);
                death.secondaryBattery = (short)(isPrimary ? 0 : GameConstants.DEFAULT_DEATH_MULTIPLIER * (byte)r.rating);
                board.UpdateObjectLocation(r.position.x, r.position.y, r.id);
                endOfTurnEvents.RemoveAll((GameEvent e) => e.primaryRobotId == r.id);
                evts.Add(death);
            }
        });
        return evts;
    }

    private List<GameEvent> processEndOfTurn()
    {
        List<GameEvent> events = new List<GameEvent>();
        endOfTurnEvents.ToList().ForEach((GameEvent e) =>
        {
            if (e is GameEvent.Damage)
            {
                GameEvent.Damage damageEvent = (GameEvent.Damage)e;
                Robot victim = GetRobot(e.primaryRobotId);
                victim.health -= damageEvent.damage;
                damageEvent.remainingHealth = victim.health;
                damageEvent.primaryBattery = damageEvent.secondaryBattery = 0;
                events.Add(damageEvent);
            }
        });
        return events;
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

    private bool Validate(List<GameEvent> events)
    {
        bool valid = true;
        events.ToList().ForEach((GameEvent g) =>
        {
            if (valid) valid = valid && Validate(events, g);
            else Invalidate(g);
        });
        return valid;
    }

    private bool Validate(List<GameEvent> events, GameEvent g)
    {
        if (g is GameEvent.Move) return Validate(events, (GameEvent.Move)g);
        else if (g is GameEvent.Push) return Validate(events, (GameEvent.Push)g);
        else if (g is GameEvent.Attack) return Validate(events, (GameEvent.Attack)g);
        return true;
    }

    private bool Validate(List<GameEvent> events, GameEvent.Move g)
    {
        int index = events.IndexOf(g);
        if (events.Count > index + 1 && events[index + 1] is GameEvent.Push) return true;
        Func<string, bool> generateBlockEvent = (string s) =>
        {
            GameEvent.Block evt = new GameEvent.Block();
            evt.deniedPos = g.destinationPos;
            evt.Transfer(g);
            evt.blockingObject = s;
            board.UpdateObjectLocation(g.sourcePos.x, g.sourcePos.y, g.primaryRobotId);
            events.RemoveAt(index);
            events.Insert(index, evt);
            return false;
        };
        if (board.IsVoid(g.destinationPos))
        {
            return generateBlockEvent("Wall");
        }
        if (board.IsBattery(g.destinationPos))
        {
            return generateBlockEvent("Battery");
        }
        if (board.IsQueue(g.destinationPos))
        {
            return generateBlockEvent("Queue");
        }
        if (board.IsSpaceOccupied(g.destinationPos))
        {
            Vector2Int diff = g.destinationPos - g.sourcePos;
            Robot occupant = GetRobot(board.GetIdOnSpace(g.destinationPos));
            if (
                g.primaryRobotId == events[0].primaryRobotId &&
                GetRobot(g.primaryRobotId).IsFacing(diff) &&
                !occupant.IsFacing(Util.Flip(diff))
            )
            {
                GameEvent.Push push = new GameEvent.Push();
                push.primaryRobotId = g.primaryRobotId;
                push.victim = occupant.id;
                events.Add(push);
                GameEvent.Move move = new GameEvent.Move();
                move.primaryRobotId = occupant.id;
                move.sourcePos = g.destinationPos;
                move.destinationPos = g.destinationPos + diff;
                board.RemoveObjectLocation(occupant.id);
                events.Add(move);
                return false;
            }
            else
            {
               return generateBlockEvent(occupant.name);
            }
        }
        return true;
    }

    private bool Validate(List<GameEvent> events, GameEvent.Push g)
    {
        int index = events.IndexOf(g);
        if (events.Count > index + 1 && events[index + 1] is GameEvent.Block)
        {
            GameEvent.Block block = new GameEvent.Block();
            block.blockingObject = ((GameEvent.Block)events[index+1]).blockingObject;
            GameEvent.Move original = (GameEvent.Move)events[index - 1];
            block.deniedPos = original.destinationPos;
            block.Transfer(original);
            board.UpdateObjectLocation(original.sourcePos.x, original.sourcePos.y, original.primaryRobotId);
            events.RemoveRange(index - 1, 3);
            events.Add(block);
            return false;
        }
        return true;
    }

    private bool Validate(List<GameEvent> events, GameEvent.Attack g)
    {
        int index = events.IndexOf(g);
        if (events.Count > index + 1 && (events[index + 1] is GameEvent.Battery || events[index+1] is GameEvent.Miss || events[index+1] is GameEvent.Damage)) return true;
        Robot attacker = GetRobot(g.primaryRobotId);
        bool hitABattery = false;
        g.locs.ToList().ForEach((Vector2Int v) =>
        {
            if (board.IsBattery(v))
            {
                bool isPrimary = primary.team.Contains(attacker);
                bool isPrimaryBase = board.IsPrimary(v);
                GameEvent.Battery evt = new GameEvent.Battery();
                evt.opponentsBase = (isPrimary && !isPrimaryBase) || (!isPrimary && isPrimaryBase);
                evt.primaryRobotId = g.primaryRobotId;
                evt.damage = attacker.attack;
                short drain = (short)(GameConstants.DEFAULT_BATTERY_MULTIPLIER * attacker.attack);
                evt.primaryBattery += isPrimaryBase ? drain : (short)0;
                evt.secondaryBattery += isPrimaryBase ? (short)0: drain;
                events.Insert(index + 1, evt);
                hitABattery = true;
            } else if (board.IsQueue(v))
            {
                g.locs = g.locs.Where((Vector2Int vv) => !vv.Equals(v)).ToArray();
            }
        });
        Robot[] victims = Array.FindAll(allRobots, (robot) => g.locs.Contains(robot.position));
        if (victims.Length == 0 && !hitABattery)
        {
            GameEvent.Miss evt = new GameEvent.Miss();
            evt.primaryRobotId = g.primaryRobotId;
            events.Insert(index + 1, evt);
        }
        else if (victims.Length > 0)
        {
            Array.ForEach(victims, (Robot r) =>
            {
                List<GameEvent> evts = attacker.Damage(r);
                events.InsertRange(index + 1, evts);
            });
        }
        return true;
    }

    private bool AreValidTogether(Dictionary<short, List<GameEvent>> idsToWantedEvents)
    {
        Dictionary<Vector2Int, List<short>> spaceToIds = new Dictionary<Vector2Int, List<short>>();
        idsToWantedEvents.Keys.ToList().ForEach((short key) =>
        {
            idsToWantedEvents[key].ForEach((GameEvent e) =>
            {
                if (e is GameEvent.Move)
                {
                    Vector2Int newspace = ((GameEvent.Move)e).destinationPos;
                    if (spaceToIds.ContainsKey(newspace)) spaceToIds[newspace].Add(key);
                    else spaceToIds[newspace] = new List<short>() { key };
                }
            });
        });
        bool valid = true;
        spaceToIds.Keys.ToList().ForEach((Vector2Int space) =>
        {
            List<short> idsWantSpace = spaceToIds[space];
            if (idsWantSpace.Count > 1)
            {
                valid = false;
                List<short> facing = idsWantSpace.FindAll((short key) => {
                    GameEvent.Move g = idsToWantedEvents[key].Find((GameEvent e) => e is GameEvent.Move && ((GameEvent.Move)e).destinationPos.Equals(space)) as GameEvent.Move;
                    return g.primaryRobotId == key && GetRobot(key).IsFacing(g.destinationPos - g.sourcePos);
                });
                string blocker = "";
                if (facing.Count == 1)
                {
                    Robot winner = GetRobot(facing[0]);
                    idsWantSpace.Remove(winner.id);
                    blocker = winner.name;
                }
                else
                {
                    blocker = "Each Other"; //TODO: Better label
                }
                idsWantSpace.ForEach((short key) =>
                {
                    List<GameEvent> wanted = idsToWantedEvents[key];
                    GameEvent.Block block = new GameEvent.Block();
                    block.blockingObject = blocker;
                    GameEvent.Move original = wanted.Find((GameEvent e) => e is GameEvent.Move && ((GameEvent.Move)e).destinationPos.Equals(space)) as GameEvent.Move;
                    block.deniedPos = original.destinationPos;
                    block.Transfer(original);
                    board.UpdateObjectLocation(original.sourcePos.x, original.sourcePos.y, original.primaryRobotId);
                    int index = wanted.IndexOf(original);
                    int size = wanted.Count - index;
                    wanted.GetRange(index, size).ForEach(Invalidate);
                    wanted.RemoveRange(index, size);
                    wanted.Add(block);
                });
            }
        });
        return valid;
    }

    private void Invalidate(GameEvent g)
    {
        if (g is GameEvent.Move) Invalidate((GameEvent.Move)g);
    }

    private void Invalidate(GameEvent.Move g)
    {
        board.UpdateObjectLocation(g.sourcePos.x, g.sourcePos.y, g.primaryRobotId);
    }

    private void Update(GameEvent g)
    {
        if (g is GameEvent.Move) Update((GameEvent.Move)g);
        else if (g is GameEvent.Rotate) Update((GameEvent.Rotate)g);
        else if (g is GameEvent.Damage) Update((GameEvent.Damage)g);
        else if (g is GameEvent.Poison) Update((GameEvent.Poison)g);
    }

    private void Update(GameEvent.Move g)
    {
        Robot r = GetRobot(g.primaryRobotId);
        r.position = g.destinationPos;
        board.UpdateObjectLocation(r.position.x, r.position.y, r.id);
    }

    private void Update(GameEvent.Rotate g)
    {
        Robot r = GetRobot(g.primaryRobotId);
        r.orientation = g.destinationDir;
    }

    private void Update(GameEvent.Damage g)
    {
        Robot r = GetRobot(g.primaryRobotId);
        r.health = g.remainingHealth;
    }

    private void Update(GameEvent.Poison g)
    {
        Robot r = GetRobot(g.primaryRobotId);
        GameEvent.Damage evt = new GameEvent.Damage();
        evt.primaryRobotId = g.primaryRobotId;
        evt.damage = 1;
        evt.remainingHealth = (short)(r.health - 1);
        endOfTurnEvents.Add(evt);
    }

    private Robot GetRobot(short id)
    {
        return Array.Find(allRobots, (Robot r) => r.id == id);
    }
}
