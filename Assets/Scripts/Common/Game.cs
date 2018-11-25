using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using UnityEngine.Networking;

public class Game
{
    internal Player primary = new Player();
    internal Player secondary = new Player();
    internal string gameSessionId;
    Robot[] allRobots = new Robot[0];
    internal List<GameEvent> endOfTurnEvents = new List<GameEvent>();
    internal Map board;
    internal short nextRobotId;
    internal byte turn = 1;

    internal Game() {}

    internal void Join(string[] t, string n, int cid)
    {
        bool isPrimary = !primary.joined;
        Robot[] robots = new Robot[t.Length];
        for (byte i = 0; i < t.Length; i++)
        {
            Robot r = Robot.create(t[i]);
            r.id = nextRobotId;
            nextRobotId++;
            r.position = Map.NULL_VEC;
            board.AddToDock(r.id, isPrimary);
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
        internal Util.Dictionary<short, RobotStat> teamStats;
        internal bool ready;
        internal bool joined;
        internal List<Command> commands;
        internal int connectionId;
        internal Player(){}
        internal Player(Robot[] t, string n)
        {
            team = t;
            teamStats = new Util.Dictionary<short, RobotStat>(t.Length);
            team.ToList().ForEach((Robot r) => teamStats.Add(r.id, new RobotStat() { name = r.name }));
            name = n;
            joined = true;
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
            { typeof(Command.Move), 0 },
            { typeof(Command.Attack), 0 },
            { typeof(Command.Special), 0 }
        };
        internal byte priority;
        internal bool isActive;
        internal RobotTurnObject(byte p)
        {
            priority = p;
        }
    }

    internal class RobotStat : MessageBase
    {
        internal string name;
        internal short spawns;
        internal short moves;
        internal short attacks;
        internal short specials;
        internal short successAttackOnRobots;
        internal short successAttackOnBattery;
        internal short successMoves;
        internal short successSpecials;
        internal short successPushes;
        internal short damageTakenFromAttacks;
        internal short damageTakenFromCollision;
        internal short damageTakenFromSpecial;
        internal short numberOfKills;
        internal short numberOfDeaths;
    }

    public byte GetTurn()
    {
        byte turnNumber = turn;
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
        Array.ForEach(allRobots, (Robot r) => {
            RobotTurnObject rto = new RobotTurnObject(r.priority);
            rto.isActive = board.IsSpaceOccupied(r.position);
            robotIdToTurnObject[r.id] = rto;
        });
        for (int p = GameConstants.MAX_PRIORITY; p >= 0; p--)
        {
            priorityToCommands[(byte)p] = new HashSet<Command>();
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
        for (int p = GameConstants.MAX_PRIORITY; p >= 0; p--)
        {
            HashSet<Command> currentCmds = priorityToCommands[(byte)p];
            List<GameEvent> priorityEvents = (p == 0 ? processEndOfTurn() : new List<GameEvent>());

            priorityEvents.AddRange(processCommands(currentCmds, robotIdToTurnObject, Command.SPAWN_COMMAND_ID));
            priorityEvents.AddRange(processCommands(currentCmds, robotIdToTurnObject, Command.MOVE_COMMAND_ID));
            priorityEvents.AddRange(processCommands(currentCmds, robotIdToTurnObject, Command.ATTACK_COMMAND_ID));
            priorityEvents.AddRange(processCommands(currentCmds, robotIdToTurnObject, Command.SPECIAL_COMMAND_ID));

            processBatteryLoss(priorityEvents, (byte)p);
            events.AddRange(priorityEvents);
            if (primary.battery <= 0 || secondary.battery <= 0)
            {
                GameEvent.End e = new GameEvent.End();
                e.primaryLost = primary.battery <= 0;
                e.secondaryLost = secondary.battery <= 0;
                e.primaryBattery = Math.Max(primary.battery, (short)0);
                e.secondaryBattery = Math.Max(secondary.battery, (short)0);
                e.turnCount = turn;
                e.timeTaken = 0;
                e.primaryTeamStats = primary.teamStats;
                e.secondaryTeamStats = secondary.teamStats;
                events.Add(e);
                break;
            }
        }
        return events;
    }

    private List<GameEvent> processCommands(HashSet<Command> allCommands, Dictionary<short, RobotTurnObject> robotIdToTurnObject, byte t)
    {
        List<GameEvent> events = new List<GameEvent>();
        HashSet<Command> commands = new HashSet<Command>(allCommands.Where((Command c) => c.commandId == t));
        commands.ToList().ForEach((Command c) =>
        {
            bool isPrimary = c.owner.Equals(primary.name);
            Robot primaryRobot = GetRobot(c.robotId);
            if (!robotIdToTurnObject[c.robotId].isActive && !(c is Command.Spawn))
            {
                commands.Remove(c);
            }
        });

        Dictionary<short, List<GameEvent>> idsToWantedEvents = new Dictionary<short, List<GameEvent>>();
        commands.ToList().ForEach((Command c) =>
        {
            Robot primaryRobot = GetRobot(c.robotId);
            bool isPrimary = c.owner.Equals(primary.name);
            if (c is Command.Spawn)
            {
                idsToWantedEvents[c.robotId] = primaryRobot.Spawn(board.GetQueuePosition(c.direction, isPrimary), isPrimary);
            } else if (c is Command.Move)
            {
                board.RemoveObjectLocation(c.robotId);
                idsToWantedEvents[c.robotId] = primaryRobot.Move(c.direction, isPrimary);
            } else if (c is Command.Attack)
            {
                idsToWantedEvents[c.robotId] = primaryRobot.Attack(c.direction, isPrimary);
            }
        });

        Stopwatch sw = new Stopwatch();
        sw.Start();
        bool valid = false;
        while (!valid)
        {
            if (sw.ElapsedMilliseconds > 5000) throw new ZException("Commands to Events caught in Infinite loop");
            valid = true;
            idsToWantedEvents.Keys.ToList().ForEach((short id) =>
            {
                valid = valid && Validate(idsToWantedEvents[id]);
            });
            if (!valid) continue;
            valid = AreValidTogether(idsToWantedEvents);
        }
        List<short> keys = idsToWantedEvents.Keys.ToList();
        keys.Sort();
        keys.ForEach((short k) => events.AddRange(idsToWantedEvents[k]));
        events.ForEach(Update);

        Func<Robot[], bool, List<GameEvent>> processPriorityFinish = (Robot[] team, bool isPrimary) =>
        {
            List<GameEvent> evts = new List<GameEvent>();
            Array.ForEach(team, (Robot r) =>
            {
                if (r.health <= 0)
                {
                    board.AddToDock(r.id, isPrimary);
                    board.RemoveObjectLocation(r.id);
                    GameEvent.Death death = new GameEvent.Death();
                    r.health = death.returnHealth = r.startingHealth;
                    r.position = Map.NULL_VEC;
                    death.primaryRobotId = r.id;
                    death.primaryBattery = (short)(isPrimary ? GameConstants.DEFAULT_DEATH_MULTIPLIER * (byte)r.rating : 0);
                    death.secondaryBattery = (short)(isPrimary ? 0 : GameConstants.DEFAULT_DEATH_MULTIPLIER * (byte)r.rating);
                    endOfTurnEvents.RemoveAll((GameEvent e) => e.primaryRobotId == r.id);
                    evts.Add(death);
                }
            });
            return evts;
        };

        events.AddRange(processPriorityFinish(primary.team, true));
        events.AddRange(processPriorityFinish(secondary.team, false));
        robotIdToTurnObject.Keys.ToList().ForEach((short k) => robotIdToTurnObject[k].isActive = board.IsSpaceOccupied(GetRobot(k).position));
        if (events.Count > 0)
        {
            GameEvent.Resolve resolve = new GameEvent.Resolve();
            resolve.commandType = t;
            events.Add(resolve);
        }
        return events;
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
        if (events.Count > 0) events.Add(new GameEvent.Resolve());
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
        else if (g is GameEvent.Spawn) return Validate(events, (GameEvent.Spawn)g);
        return true;
    }

    private bool Validate(List<GameEvent> events, GameEvent.Move g)
    {
        int index = events.IndexOf(g);
        if (events.Count > index + 1 && events[index + 1] is GameEvent.Push) return true;
        if (!g.success) return true;
        Func<string, bool> generateBlockEvent = (string s) =>
        {
            Robot r = GetRobot(g.primaryRobotId);
            g.success = false;
            GameEvent.Block evt = new GameEvent.Block();
            evt.deniedPos = g.destinationPos;
            evt.primaryRobotId = g.primaryRobotId;
            evt.blockingObject = s;
            board.UpdateObjectLocation(g.sourcePos.x, g.sourcePos.y, g.primaryRobotId);
            events.Insert(index+1, evt);
            GameEvent.Damage evt2 = new GameEvent.Damage();
            evt2.damage = 1;
            evt2.primaryRobotId = g.primaryRobotId;
            evt2.remainingHealth = (short)(r.health - 1);
            events.Insert(index + 2, evt2);
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
        if (board.IsSpaceOccupied(g.destinationPos))
        {
            Vector2Int diff = g.destinationPos - g.sourcePos;
            Robot occupant = GetRobot(board.GetIdOnSpace(g.destinationPos));
            if (g.primaryRobotId == events[0].primaryRobotId)
            {
                GameEvent.Push push = new GameEvent.Push();
                push.primaryRobotId = g.primaryRobotId;
                push.victim = occupant.id;
                push.direction = diff;
                events.Add(push);
                GameEvent.Damage d1 = new GameEvent.Damage();
                d1.damage = 1;
                d1.primaryRobotId = g.primaryRobotId;
                d1.remainingHealth = (short)(GetRobot(g.primaryRobotId).health - 1);
                events.Add(d1);
                GameEvent.Damage d2 = new GameEvent.Damage();
                d2.damage = 1;
                d2.primaryRobotId = occupant.id;
                d2.remainingHealth = (short)(occupant.health - 1);
                events.Add(d2);
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
                generateBlockEvent(occupant.name);
                GameEvent.Damage d2 = new GameEvent.Damage();
                d2.damage = 1;
                d2.primaryRobotId = occupant.id;
                d2.remainingHealth = (short)(occupant.health - 1);
                events.Add(d2);
                return false;
            }
        }
        return true;
    }

    private bool Validate(List<GameEvent> events, GameEvent.Push g)
    {
        int index = events.IndexOf(g);
        if (events.Count > index + 4 && events[index + 4] is GameEvent.Block && g.success)
        {
            GameEvent.Block block = new GameEvent.Block();
            block.blockingObject = ((GameEvent.Block)events[index+4]).blockingObject;
            GameEvent.Move original = (GameEvent.Move)events[index - 1];
            original.success = false;
            g.success = false;
            block.deniedPos = original.destinationPos;
            block.primaryRobotId = original.primaryRobotId;
            board.UpdateObjectLocation(original.sourcePos.x, original.sourcePos.y, original.primaryRobotId);
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
                bool isPrimaryBase = board.IsPrimary(v);
                GameEvent.Battery evt = new GameEvent.Battery();
                evt.isPrimary = isPrimaryBase;
                evt.primaryRobotId = g.primaryRobotId;
                evt.damage = attacker.attack;
                short drain = (short)(GameConstants.DEFAULT_BATTERY_MULTIPLIER * attacker.attack);
                evt.primaryBattery += isPrimaryBase ? drain : (short)0;
                evt.secondaryBattery += isPrimaryBase ? (short)0: drain;
                events.Insert(index + 1, evt);
                hitABattery = true;
            }
        });
        Robot[] victims = Array.FindAll(allRobots, (robot) => g.locs.Contains(robot.position));
        if (victims.Length == 0 && !hitABattery)
        {
            GameEvent.Miss evt = new GameEvent.Miss();
            evt.primaryRobotId = g.primaryRobotId;
            evt.locs = g.locs;
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

    private bool Validate(List<GameEvent> events, GameEvent.Spawn g)
    {
        int index = events.IndexOf(g);
        if (board.IsSpaceOccupied(g.destinationPos))
        {
            GameEvent.Block evt = new GameEvent.Block();
            evt.deniedPos = g.destinationPos;
            evt.Transfer(g);
            evt.blockingObject = GetRobot(board.GetIdOnSpace(g.destinationPos)).name;
            events.RemoveAt(index);
            events.Insert(index, evt);
            return false;
        }
        return true;
    }

    private bool AreValidTogether(Dictionary<short, List<GameEvent>> idsToWantedEvents)
    {
        bool valid = true;
        Func<string, Vector2Int, Action<short>> generateBlockEvent = (string blocker, Vector2Int space) => new Action<short>((short key) =>
        {
            List<GameEvent> wanted = idsToWantedEvents[key];
            GameEvent.Block block = new GameEvent.Block();
            block.blockingObject = blocker;
            GameEvent original = wanted.Find((GameEvent e) => 
                (e is GameEvent.Move && ((GameEvent.Move)e).destinationPos.Equals(space)) ||
                (e is GameEvent.Spawn && ((GameEvent.Spawn)e).destinationPos.Equals(space))
            );
            block.deniedPos = original is GameEvent.Move ? ((GameEvent.Move)original).destinationPos : ((GameEvent.Spawn)original).destinationPos;
            block.primaryRobotId = original.primaryRobotId;
            int index = wanted.IndexOf(original);
            int size = wanted.Count - index;
            wanted.GetRange(index, size).ForEach(Invalidate);
            wanted.RemoveRange(index + 1, size - 1);
            wanted.Add(block);
            if (original is GameEvent.Move)
            {
                original.success = false;
                board.UpdateObjectLocation(((GameEvent.Move)original).sourcePos.x, ((GameEvent.Move)original).sourcePos.y, original.primaryRobotId);
                GameEvent.Damage d = new GameEvent.Damage();
                d.damage = 1;
                d.remainingHealth = (short)(GetRobot(original.primaryRobotId).health - 1);
                d.primaryRobotId = original.primaryRobotId;
                wanted.Add(d);
            }
            else
            {
                board.RemoveObjectLocation(original.primaryRobotId);
            }
        });

        //First do swapping positions check
        idsToWantedEvents.Keys.ToList().ForEach((short key1) =>
        {
            idsToWantedEvents.Keys.ToList().ForEach((short key2) =>
            {
                Robot r1 = GetRobot(key1);
                Robot r2 = GetRobot(key2);
                GameEvent.Move m1 = idsToWantedEvents[key1].Find((GameEvent g) => {
                    return g is GameEvent.Move && ((GameEvent.Move)g).destinationPos.Equals(r2.position) && g.success;
                }) as GameEvent.Move;
                GameEvent.Move m2 = idsToWantedEvents[key2].Find((GameEvent g) => {
                    return g is GameEvent.Move && ((GameEvent.Move)g).destinationPos.Equals(r1.position) && g.success;
                }) as GameEvent.Move;
                if (m1 != null && m2 != null)
                {
                    generateBlockEvent(r2.name, m1.destinationPos)(key1);
                    generateBlockEvent(r1.name, m2.destinationPos)(key2);
                    valid = false;
                }
            });
        });

        //Then do multiple robots contest for one position check
        Dictionary<Vector2Int, List<short>> spaceToIds = new Dictionary<Vector2Int, List<short>>();
        idsToWantedEvents.Keys.ToList().ForEach((short key) =>
        {
            idsToWantedEvents[key].ForEach((GameEvent e) =>
            {
                Vector2Int newspace = Map.NULL_VEC;
                if (e is GameEvent.Move)
                {
                    newspace = ((GameEvent.Move)e).destinationPos;
                } else if (e is GameEvent.Spawn)
                {
                    newspace = ((GameEvent.Spawn)e).destinationPos;
                }
                if (!newspace.Equals(Map.NULL_VEC) && e.success)
                {
                    if (spaceToIds.ContainsKey(newspace)) spaceToIds[newspace].Add(key);
                    else spaceToIds[newspace] = new List<short>() { key };
                }
            });
        });
        spaceToIds.Keys.ToList().ForEach((Vector2Int space) =>
        {
            List<short> idsWantSpace = spaceToIds[space];
            if (idsWantSpace.Count > 1)
            {
                valid = false;
                idsWantSpace.ForEach(generateBlockEvent("Each Other", space));
            }
        });

        //Then do multiple damage on one robot check
        Dictionary<short, List<GameEvent.Damage>> idsToDamages = new Dictionary<short, List<GameEvent.Damage>>();
        idsToWantedEvents.Values.ToList().ForEach((List<GameEvent> evts) =>
        {
            evts.ForEach((GameEvent g) => {
                if (g is GameEvent.Damage) {
                    if (!idsToDamages.ContainsKey(g.primaryRobotId))
                    {
                        idsToDamages[g.primaryRobotId] = new List<GameEvent.Damage>() { (GameEvent.Damage)g };
                    } else
                    {
                        idsToDamages[g.primaryRobotId].Add((GameEvent.Damage)g);
                    }
                }
            });
        });
        idsToDamages.Values.ToList().ForEach((List<GameEvent.Damage> damages) =>
        {
            damages.Sort((GameEvent.Damage d1, GameEvent.Damage d2) =>
            {
                short k1 = idsToWantedEvents.Keys.ToList().Find((short k) => idsToWantedEvents[k].Contains(d1));
                short k2 = idsToWantedEvents.Keys.ToList().Find((short k) => idsToWantedEvents[k].Contains(d2));
                return k1 - k2;
            });
            for (int i = 1; i < damages.Count; i++)
            {
                damages[i].remainingHealth = (short)(damages[i - 1].remainingHealth - damages[i].damage);
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
        else if (g is GameEvent.Damage) Update((GameEvent.Damage)g);
        else if (g is GameEvent.Poison) Update((GameEvent.Poison)g);
        else if (g is GameEvent.Spawn) Update((GameEvent.Spawn)g);
    }

    private void Update(GameEvent.Move g)
    {
        if (!g.success) return;
        Robot r = GetRobot(g.primaryRobotId);
        r.position = g.destinationPos;
        board.UpdateObjectLocation(r.position.x, r.position.y, r.id);
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

    private void Update(GameEvent.Spawn g)
    {
        Robot r = GetRobot(g.primaryRobotId);
        r.position = g.destinationPos;
        board.UpdateObjectLocation(r.position.x, r.position.y, r.id);
    }

    private Robot GetRobot(short id)
    {
        return Array.Find(allRobots, (Robot r) => r.id == id);
    }
}
