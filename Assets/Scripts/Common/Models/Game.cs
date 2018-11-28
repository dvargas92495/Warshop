using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class Game
{
    public Player primary = new Player();
    public Player secondary = new Player();
    internal string gameSessionId;
    Robot[] allRobots = new Robot[0];
    internal Util.List<GameEvent> endOfTurnEvents = new Util.List<GameEvent>();
    public Map board;
    internal short nextRobotId;
    internal byte turn = 1;

    public Game() {}

    public void Join(string[] t, string n, int cid)
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

    internal Util.List<int> connectionIds()
    {
        Util.List<int> ids = new Util.List<int>();
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
        public short battery = GameConstants.POINTS_TO_WIN;
        public Robot[] team;
        internal Util.Dictionary<short, RobotStat> teamStats;
        internal bool ready;
        internal bool joined;
        internal Util.List<Command> commands;
        internal int connectionId;
        internal Player(){}
        internal Player(Robot[] t, string n)
        {
            team = t;
            teamStats = new Util.Dictionary<short, RobotStat>(t.Length);
            Util.ForEach(team, r => teamStats.Add(r.id, new RobotStat() { name = r.name }));
            name = n;
            joined = true;
        }
        public void StoreCommands(Util.List<Command> cmds)
        {
            commands = cmds;
            commands.ForEach((Command c) => c.owner = name);
            ready = true;
        }
        internal Util.List<Command> FetchCommands()
        {
            Util.List<Command> cmds = new Util.List<Command>(commands);
            ready = false;
            commands.Clear();
            return cmds;
        }
    }

    internal class RobotTurnObject
    {
        internal Util.Dictionary<byte, byte> num = new Util.Dictionary<byte, byte>(Command.NUM_TYPES);
        internal byte priority;
        internal bool isActive;
        internal RobotTurnObject(byte p)
        {
            priority = p;
            num.Add(Command.SPAWN_COMMAND_ID, 0);
            num.Add(Command.MOVE_COMMAND_ID, 0);
            num.Add(Command.ATTACK_COMMAND_ID, 0);
            num.Add(Command.SPECIAL_COMMAND_ID, 0);
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

    public Util.List<GameEvent> CommandsToEvents()
    {
        Util.List<Command> commands = new Util.List<Command>();
        commands.Add(primary.FetchCommands());
        commands.Add(secondary.FetchCommands());
        Util.Dictionary<byte, Util.Set<Command>> priorityToCommands = new Util.Dictionary<byte, Util.Set<Command>>(GameConstants.MAX_PRIORITY + 1);
        Util.Dictionary<short, RobotTurnObject> robotIdToTurnObject = new Util.Dictionary<short, RobotTurnObject>(primary.team.Length + secondary.team.Length);
        Util.ForEach(allRobots, r => {
            RobotTurnObject rto = new RobotTurnObject(r.priority);
            rto.isActive = board.IsSpaceOccupied(r.position);
            robotIdToTurnObject.Add(r.id, rto);
        });
        for (int p = GameConstants.MAX_PRIORITY; p >= 0; p--)
        {
            priorityToCommands.Add((byte)p, new Util.Set<Command>(robotIdToTurnObject.GetLength()));
        }
        commands.ForEach((Command c) =>
        {
            if (robotIdToTurnObject.Get(c.robotId).priority > 0)
            {
                priorityToCommands.Get(robotIdToTurnObject.Get(c.robotId).priority).Add(c);
                robotIdToTurnObject.Get(c.robotId).priority--;
            }
        });
        Util.List<GameEvent> events = new Util.List<GameEvent>();
        for (int p = GameConstants.MAX_PRIORITY; p >= 0; p--)
        {
            Util.Set<Command> currentCmds = priorityToCommands.Get((byte)p);
            Util.List<GameEvent> priorityEvents = p == 0 ? processEndOfTurn() : new Util.List<GameEvent>();

            priorityEvents.Add(processCommands(currentCmds, robotIdToTurnObject, Command.SPAWN_COMMAND_ID));
            priorityEvents.Add(processCommands(currentCmds, robotIdToTurnObject, Command.MOVE_COMMAND_ID));
            priorityEvents.Add(processCommands(currentCmds, robotIdToTurnObject, Command.ATTACK_COMMAND_ID));
            priorityEvents.Add(processCommands(currentCmds, robotIdToTurnObject, Command.SPECIAL_COMMAND_ID));

            processBatteryLoss(priorityEvents, (byte)p);
            events.Add(priorityEvents);
            if (primary.battery <= 0 || secondary.battery <= 0)
            {
                GameEvent.End e = new GameEvent.End();
                e.primaryLost = primary.battery <= 0;
                e.secondaryLost = secondary.battery <= 0;
                e.primaryBattery = (short)Util.Max(primary.battery, 0);
                e.secondaryBattery = (short)Util.Max(secondary.battery, 0);
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

    private Util.List<GameEvent> processCommands(Util.Set<Command> allCommands, Util.Dictionary<short, RobotTurnObject> robotIdToTurnObject, byte t)
    {
        Util.List<GameEvent> events = new Util.List<GameEvent>();
        Util.Set<Command> commands = new Util.Set<Command>(allCommands.Filter(c => c.commandId == t));
        commands.ForEach((Command c) =>
        {
            bool isPrimary = c.owner.Equals(primary.name);
            Robot primaryRobot = GetRobot(c.robotId);
            if (!robotIdToTurnObject.Get(c.robotId).isActive && !(c is Command.Spawn))
            {
                commands.Remove(c);
            }
        });

        Util.Dictionary<short, Util.List<GameEvent>> idsToWantedEvents = new Util.Dictionary<short, Util.List<GameEvent>>(robotIdToTurnObject.GetLength());
        commands.ForEach((Command c) =>
        {
            Robot primaryRobot = GetRobot(c.robotId);
            bool isPrimary = c.owner.Equals(primary.name);
            if (c is Command.Spawn)
            {
                idsToWantedEvents.Add(c.robotId, primaryRobot.Spawn(board.GetQueuePosition(c.direction, isPrimary), isPrimary));
            } else if (c is Command.Move)
            {
                board.RemoveObjectLocation(c.robotId);
                idsToWantedEvents.Add(c.robotId, primaryRobot.Move(c.direction, isPrimary));
            } else if (c is Command.Attack)
            {
                idsToWantedEvents.Add(c.robotId, primaryRobot.Attack(c.direction, isPrimary));
            }
        });

        bool valid = false;
        while (!valid)
        {
            valid = true;
            idsToWantedEvents.ForEachValue(evts =>
            {
                valid = valid && Validate(evts);
            });
            if (!valid) continue;
            valid = AreValidTogether(idsToWantedEvents);
        }
        idsToWantedEvents.ForEachValue(evts => events.Add(evts));
        events.ForEach(Update);

        Util.ReturnAction<Robot[], bool, Util.List<GameEvent>> processPriorityFinish = (Robot[] team, bool isPrimary) =>
        {
            Util.List<GameEvent> evts = new Util.List<GameEvent>();
            Util.ForEach(team, (Robot r) =>
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

        events.Add(processPriorityFinish(primary.team, true));
        events.Add(processPriorityFinish(secondary.team, false));
        robotIdToTurnObject.ForEach((k, rto) => rto.isActive = board.IsSpaceOccupied(GetRobot(k).position));
        if (events.GetLength() > 0)
        {
            GameEvent.Resolve resolve = new GameEvent.Resolve();
            resolve.commandType = t;
            events.Add(resolve);
        }
        return events;
    }

    private Util.List<GameEvent> processEndOfTurn()
    {
        Util.List<GameEvent> events = new Util.List<GameEvent>();
        endOfTurnEvents.ForEach((GameEvent e) =>
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
        if (events.GetLength() > 0) events.Add(new GameEvent.Resolve());
        return events;
    }

    private void processBatteryLoss(Util.List<GameEvent> evts, byte p)
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

    private bool Validate(Util.List<GameEvent> events)
    {
        bool valid = true;
        events.ForEach((GameEvent g) =>
        {
            if (valid) valid = valid && Validate(events, g);
            else Invalidate(g);
        });
        return valid;
    }

    private bool Validate(Util.List<GameEvent> events, GameEvent g)
    {
        if (g is GameEvent.Move) return Validate(events, (GameEvent.Move)g);
        else if (g is GameEvent.Push) return Validate(events, (GameEvent.Push)g);
        else if (g is GameEvent.Attack) return Validate(events, (GameEvent.Attack)g);
        else if (g is GameEvent.Spawn) return Validate(events, (GameEvent.Spawn)g);
        return true;
    }

    private bool Validate(Util.List<GameEvent> events, GameEvent.Move g)
    {
        int index = events.FindIndex(g);
        if (events.GetLength() > index + 1 && events.Get(index + 1) is GameEvent.Push) return true;
        if (!g.success) return true;
        Util.ReturnAction<string, bool> generateBlockEvent = (string s) =>
        {
            Robot r = GetRobot(g.primaryRobotId);
            g.success = false;
            GameEvent.Block evt = new GameEvent.Block();
            evt.deniedPos = g.destinationPos;
            evt.primaryRobotId = g.primaryRobotId;
            evt.blockingObject = s;
            board.UpdateObjectLocation(g.sourcePos.x, g.sourcePos.y, g.primaryRobotId);
            events.Add(evt, index+1);
            GameEvent.Damage evt2 = new GameEvent.Damage();
            evt2.damage = 1;
            evt2.primaryRobotId = g.primaryRobotId;
            evt2.remainingHealth = (short)(r.health - 1);
            events.Add(evt2, index + 2);
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
            if (g.primaryRobotId == events.Get(0).primaryRobotId)
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

    private bool Validate(Util.List<GameEvent> events, GameEvent.Push g)
    {
        int index = events.FindIndex(g);
        if (events.GetLength() > index + 4 && events.Get(index + 4) is GameEvent.Block && g.success)
        {
            GameEvent.Block block = new GameEvent.Block();
            block.blockingObject = ((GameEvent.Block)events.Get(index+4)).blockingObject;
            GameEvent.Move original = (GameEvent.Move)events.Get(index - 1);
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

    private bool Validate(Util.List<GameEvent> events, GameEvent.Attack g)
    {
        int index = events.FindIndex(g);
        if (events.GetLength() > index + 1 && (events.Get(index + 1) is GameEvent.Battery || events.Get(index+1) is GameEvent.Miss || events.Get(index+1) is GameEvent.Damage)) return true;
        Robot attacker = GetRobot(g.primaryRobotId);
        bool hitABattery = false;
        Util.ForEach(g.locs, (Vector2Int v) =>
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
                events.Add(evt, index + 1);
                hitABattery = true;
            }
        });
        Robot[] victims = Util.Filter(allRobots, (robot) => Util.Contains(g.locs, robot.position));
        if (victims.Length == 0 && !hitABattery)
        {
            GameEvent.Miss evt = new GameEvent.Miss();
            evt.primaryRobotId = g.primaryRobotId;
            evt.locs = g.locs;
            events.Add(evt, index + 1);
        }
        else if (victims.Length > 0)
        {
            Util.ForEach(victims, (Robot r) =>
            {
                Util.List<GameEvent> evts = attacker.Damage(r);
                events.Add(evts, index + 1);
            });
        }
        return true;
    }

    private bool Validate(Util.List<GameEvent> events, GameEvent.Spawn g)
    {
        int index = events.FindIndex(g);
        if (board.IsSpaceOccupied(g.destinationPos))
        {
            GameEvent.Block evt = new GameEvent.Block();
            evt.deniedPos = g.destinationPos;
            evt.Transfer(g);
            evt.blockingObject = GetRobot(board.GetIdOnSpace(g.destinationPos)).name;
            events.RemoveAt(index);
            events.Add(evt, index);
            return false;
        }
        return true;
    }

    private bool AreValidTogether(Util.Dictionary<short, Util.List<GameEvent>> idsToWantedEvents)
    {
        bool valid = true;
        Util.ReturnAction<string, Vector2Int, UnityAction<short>> generateBlockEvent = (string blocker, Vector2Int space) => new UnityAction<short>((short key) =>
        {
            Util.List<GameEvent> wanted = idsToWantedEvents.Get(key);
            GameEvent.Block block = new GameEvent.Block();
            block.blockingObject = blocker;
            GameEvent original = wanted.Find((GameEvent e) =>
                (e is GameEvent.Move && ((GameEvent.Move)e).destinationPos.Equals(space)) ||
                (e is GameEvent.Spawn && ((GameEvent.Spawn)e).destinationPos.Equals(space))
            );
            block.deniedPos = original is GameEvent.Move ? ((GameEvent.Move)original).destinationPos : ((GameEvent.Spawn)original).destinationPos;
            block.primaryRobotId = original.primaryRobotId;
            int index = wanted.FindIndex(original);
            int size = wanted.GetLength() - index;
            wanted.Get(index, size).ForEach(Invalidate);
            wanted.RemoveAt(index + 1, size - 1);
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
        idsToWantedEvents.ForEach((key1, evts1) =>
        {
            idsToWantedEvents.ForEach((key2, evts2) =>
            {
                Robot r1 = GetRobot(key1);
                Robot r2 = GetRobot(key2);
                GameEvent.Move m1 = evts1.Find((GameEvent g) => {
                    return g is GameEvent.Move && ((GameEvent.Move)g).destinationPos.Equals(r2.position) && g.success;
                }) as GameEvent.Move;
                GameEvent.Move m2 = evts2.Find((GameEvent g) => {
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
        Util.Dictionary<Vector2Int, Util.List<short>> spaceToIds = new Util.Dictionary<Vector2Int, Util.List<short>>(board.spaces.Length);
        idsToWantedEvents.ForEach((key, evts) =>
        {
            evts.ForEach((GameEvent e) =>
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
                    if (spaceToIds.Contains(newspace)) spaceToIds.Get(newspace).Add(key);
                    else spaceToIds.Add(newspace, new Util.List<short>(key));
                }
            });
        });
        spaceToIds.ForEach((space, idsWantSpace) =>
        {
            if (idsWantSpace.GetLength() > 1)
            {
                valid = false;
                idsWantSpace.ForEach(generateBlockEvent("Each Other", space));
            }
        });

        //Then do multiple damage on one robot check
        Util.Dictionary<short, Util.List<GameEvent.Damage>> idsToDamages = new Util.Dictionary<short, Util.List<GameEvent.Damage>>(allRobots.Length);
        idsToWantedEvents.ForEachValue(evts =>
        {
            evts.ForEach((GameEvent g) => {
                if (g is GameEvent.Damage) {
                    if (!idsToDamages.Contains(g.primaryRobotId))
                    {
                        idsToDamages.Add(g.primaryRobotId, new Util.List<GameEvent.Damage>((GameEvent.Damage)g));
                    } else
                    {
                        idsToDamages.Get(g.primaryRobotId).Add((GameEvent.Damage)g);
                    }
                }
            });
        });
        idsToDamages.ForEachValue((Util.List<GameEvent.Damage> damages) =>
        {
            for (int i = 1; i < damages.GetLength(); i++)
            {
                damages.Get(i).remainingHealth = (short)(damages.Get(i - 1).remainingHealth - damages.Get(i).damage);
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
        return Util.Find(allRobots, (Robot r) => r.id == id);
    }
}
