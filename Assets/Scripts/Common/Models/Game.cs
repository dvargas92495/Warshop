using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class Game
{
    public Player primary = new Player();
    public Player secondary = new Player();
    internal string gameSessionId;
    internal List<GameEvent> endOfTurnEvents = new List<GameEvent>();
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
        if (!isPrimary)
        {
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
        resp.myTeam = (forPrimary ? primary.team : secondary.team).ToArray();
        resp.opponentTeam = (forPrimary ? secondary.team : primary.team).ToArray();
        resp.opponentname = (forPrimary ? secondary.name : primary.name);
        return resp;
    }

    public class Player
    {
        internal string name;
        public short battery = GameConstants.POINTS_TO_WIN;
        public List<Robot> team;
        internal Dictionary<short, RobotStat> teamStats;
        internal bool ready;
        internal bool joined;
        internal List<Command> commands;
        internal int connectionId;
        internal Player(){}
        internal Player(Robot[] t, string n)
        {
            team = new List<Robot>(t);
            teamStats = new Dictionary<short, RobotStat>(t.Length);
            team.ForEach(r => teamStats.Add(r.id, new RobotStat() { name = r.name }));
            name = n;
            joined = true;
        }
        public void StoreCommands(List<Command> cmds)
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
        internal short robotId;
        internal Dictionary<byte, byte> num = new Dictionary<byte, byte>(Command.TYPES.Length);
        internal byte priority;
        internal bool isActive;
        internal RobotTurnObject(short id, byte p)
        {
            robotId = id;
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

    public List<GameEvent> CommandsToEvents()
    {
        List<Command> commands = new List<Command>();
        commands.Add(primary.FetchCommands());
        commands.Add(secondary.FetchCommands());
        Dictionary<short, RobotTurnObject> robotIdToTurnObject = new Dictionary<short, RobotTurnObject>(primary.team.GetLength() + secondary.team.GetLength());
        List<Robot> allRobots = primary.team.Concat(secondary.team);
        allRobots.ForEach(r => {
            RobotTurnObject rto = new RobotTurnObject(r.id, r.priority);
            rto.isActive = !r.position.Equals(Map.NULL_VEC);
            robotIdToTurnObject.Add(r.id, rto);
        });
        List<GameEvent> events = new List<GameEvent>();
        for (int p = GameConstants.MAX_PRIORITY; p >= 0; p--)
        {
            Set<Command> currentCmds = robotIdToTurnObject.ToValueListFiltered(rto => rto.priority == p && commands.Any(c => c.robotId == rto.robotId)).ToSet(rto =>
            {
                rto.priority--;
                return commands.RemoveFirst(c => c.robotId == rto.robotId);
            });
            List<GameEvent> priorityEvents = new List<GameEvent>();

            currentCmds.ForEach((Command c) =>
            {
                Robot primaryRobot = GetRobot(c.robotId);
                if (!robotIdToTurnObject.Get(c.robotId).isActive && !(c is Command.Spawn))
                {
                    currentCmds.Remove(c);
                }
            });
            currentCmds.ForEach((Command c) =>
            {
                Robot primaryRobot = GetRobot(c.robotId);
                bool isPrimary = c.owner.Equals(primary.name);
                if (c is Command.Spawn)
                {
                    priorityEvents.Add(primaryRobot.Spawn(board.GetQueuePosition(c.direction, isPrimary), isPrimary));
                } else if (c is Command.Move)
                {
                    priorityEvents.Add(primaryRobot.Move(c.direction, isPrimary));
                } else if (c is Command.Attack)
                {
                    priorityEvents.Add(primaryRobot.Attack(c.direction, isPrimary));
                }
            });

            if (priorityEvents.GetLength() >= 0) {
                ResolveEvent resolveEvent = new ResolveEvent();
                resolveEvent.robotIdToSpawn = priorityEvents.Filter(e => e is SpawnEvent)
                                                            .Map(e => (SpawnEvent)e)
                                                            .Map(e => new Tuple<short, Vector2Int>(e.robotId, e.destinationPos));
                resolveEvent.robotIdToMove = priorityEvents.Filter(e => e is MoveEvent)
                                                        .Map(e => (MoveEvent)e)
                                                        .Map(e => new Tuple<short, Vector2Int>(e.robotId, e.destinationPos));
                List<Tuple<short, short>> robotIdToHealth = new List<Tuple<short, short>>();
                priorityEvents.Filter(e => e is AttackEvent)
                            .Map(e => (AttackEvent)e)
                            .ForEach(e => {
                                    Robot attacker = GetRobot(e.robotId);
                                    allRobots.Filter(robot => e.locs.Contains(robot.position))
                                        .ForEach(r =>
                                        {
                                            short damage = attacker.Damage(r);
                                            Tuple<short, short> robotAndHealth = robotIdToHealth.Find(t => t.GetLeft() == r.id);
                                            if(robotAndHealth == null) {
                                                robotIdToHealth.Add(new Tuple<short, short>(r.id, (short)(r.health - damage)));
                                            } else {
                                                robotAndHealth.SetRight((short)(robotAndHealth.GetRight() - damage));
                                            }
                                        });
                            });
                resolveEvent.robotIdToHealth = robotIdToHealth;
                priorityEvents.Add(resolveEvent);

                // CONFLICT RESOLUTION HERE
                
                resolveEvent.robotIdToSpawn.ForEach(t => {
                    GetRobot(t.GetLeft()).position = t.GetRight();
                });
                resolveEvent.robotIdToMove.ForEach(t => {
                    GetRobot(t.GetLeft()).position = t.GetRight();
                });
                resolveEvent.robotIdToHealth.ForEach(t => {
                    Robot r = GetRobot(t.GetLeft());
                    r.health = t.GetRight(); 
                });
            }
            
            robotIdToTurnObject.ForEach((k, rto) => rto.isActive = !GetRobot(k).position.Equals(Map.NULL_VEC));

            priorityEvents.Add(processPriorityFinish(primary.team, true));
            priorityEvents.Add(processPriorityFinish(secondary.team, false));

            priorityEvents.ForEach(e =>
            {
                e.priority = (byte)p;
                primary.battery -= e.primaryBatteryCost;
                secondary.battery -= e.secondaryBatteryCost;
            });
            events.Add(priorityEvents);
            if (primary.battery <= 0 || secondary.battery <= 0)
            {
                GameEvent.End e = new GameEvent.End();
                e.primaryLost = primary.battery <= 0;
                e.secondaryLost = secondary.battery <= 0;
                e.primaryBatteryCost = (short)Math.Max(primary.battery, 0);
                e.secondaryBatteryCost = (short)Math.Max(secondary.battery, 0);
                e.turnCount = turn;
                e.primaryTeamStats = primary.teamStats;
                e.secondaryTeamStats = secondary.teamStats;
                events.Add(e);
                break;
            }
        }
        return events;
    }
    private List<GameEvent> processPriorityFinish(List<Robot> team, bool isPrimary)
    {
        List<GameEvent> evts = new List<GameEvent>();
        team.ForEach(r =>
        {
            if (r.health <= 0)
            {
                board.AddToDock(r.id, isPrimary);
                GameEvent.Death death = new GameEvent.Death();
                r.health = death.returnHealth = r.startingHealth;
                r.position = Map.NULL_VEC;
                death.robotId = r.id;
                death.primaryBatteryCost = (short)(isPrimary ? GameConstants.DEFAULT_DEATH_MULTIPLIER * (byte)r.rating : 0);
                death.secondaryBatteryCost = (short)(isPrimary ? 0 : GameConstants.DEFAULT_DEATH_MULTIPLIER * (byte)r.rating);
                endOfTurnEvents.Map(g => (DamageEvent)g).RemoveAll(e => e.robotId == r.id);
                evts.Add(death);
            }
        });
        return evts;
    }

    private bool Validate(List<GameEvent> events)
    {
        return events.Reduce(true, (valid, g) => valid && Validate(events, g));
    }

    private bool Validate(List<GameEvent> events, GameEvent g)
    {
        if (g is MoveEvent) return Validate(events, (MoveEvent)g);
        else if (g is PushEvent) return Validate(events, (PushEvent)g);
        else if (g is AttackEvent) return Validate(events, (AttackEvent)g);
        else if (g is SpawnEvent) return Validate(events, (SpawnEvent)g);
        return true;
    }

    private bool Validate(List<GameEvent> events, MoveEvent g)
    {
        int index = events.FindIndex(g);
        if (!g.success) return true;
        ReturnAction<string, bool> generateBlockEvent = (string s) =>
        {
            Robot r = GetRobot(g.robotId);
            g.success = false;
            BlockEvent evt = new BlockEvent();
            evt.deniedPos = g.destinationPos;
            evt.robotId = g.robotId;
            evt.blockingObject = s;
            events.Add(evt, index+1);
            DamageEvent evt2 = new DamageEvent();
            evt2.damage = 1;
            evt2.robotId = g.robotId;
            evt2.remainingHealth = (short)(r.health - 1);
            events.Add(evt2, index + 2);
            return false;
        };
        if (board.IsVoid(g.destinationPos))
        {
            return generateBlockEvent(BlockEvent.WALL);
        }
        if (board.IsBattery(g.destinationPos))
        {
            return generateBlockEvent(BlockEvent.BATTERY);
        }
        return true;
    }

    private bool Validate(List<GameEvent> events, PushEvent g)
    {
        int index = events.FindIndex(g);
        if (events.GetLength() > index + 4 && events.Get(index + 4) is BlockEvent && g.success)
        {
            BlockEvent block = new BlockEvent();
            block.blockingObject = ((BlockEvent)events.Get(index+4)).blockingObject;
            MoveEvent original = (MoveEvent)events.Get(index - 1);
            original.success = false;
            g.success = false;
            block.deniedPos = original.destinationPos;
            block.robotId = original.robotId;
            events.Add(block);
            return false;
        }
        return true;
    }

    private bool Validate(List<GameEvent> events, AttackEvent g)
    {
        int index = events.FindIndex(g);
        if (events.GetLength() > index + 1 && (/*events.Get(index + 1) is GameEvent.Battery || */events.Get(index+1) is MissEvent || events.Get(index+1) is DamageEvent)) return true;
        Robot attacker = GetRobot(g.robotId);
        bool hitABattery = false;
        /*
        g.locs.ForEach(v =>
        {
            if (board.IsBattery(v))
            {
                bool isPrimaryBase = board.IsPrimary(v);
                GameEvent.Battery evt = new GameEvent.Battery();
                evt.isPrimary = isPrimaryBase;
                evt.damage = attacker.attack;
                short drain = (short)(GameConstants.DEFAULT_BATTERY_MULTIPLIER * attacker.attack);
                evt.primaryBatteryCost += isPrimaryBase ? drain : (short)0;
                evt.secondaryBatteryCost += isPrimaryBase ? (short)0: drain;
                events.Add(evt, index + 1);
                hitABattery = true;
            }
        });
        */
        List<Robot> victims = primary.team.Concat(secondary.team).Filter(robot => g.locs.Contains(robot.position));
        if (victims.GetLength() == 0 && !hitABattery)
        {
            MissEvent evt = new MissEvent();
            evt.robotId = g.robotId;
            evt.locs = g.locs;
            events.Add(evt, index + 1);
        }
        return true;
    }

    private bool Validate(List<GameEvent> events, SpawnEvent g)
    {
        if (!g.success) return true;
        int index = events.FindIndex(g);
        Robot occupant = GetRobot(g.destinationPos);
        if (occupant != null)
        {
            BlockEvent evt = new BlockEvent();
            evt.deniedPos = g.destinationPos;
            evt.robotId = g.robotId;
            evt.blockingObject = occupant.name;
            g.success = false;
            events.Add(evt, index+1);
            return false;
        }
        return true;
    }

    private bool AreValidTogether(Dictionary<short, List<GameEvent>> idsToWantedEvents)
    {
        bool valid = true;
        ReturnAction<string, Vector2Int, UnityAction<short>> generateBlockEvent = (string blocker, Vector2Int space) => new UnityAction<short>((short key) =>
        {
            List<GameEvent> wanted = idsToWantedEvents.Get(key);
            BlockEvent block = new BlockEvent();
            block.blockingObject = blocker;
            GameEvent original = wanted.Find((GameEvent e) =>
                (e is MoveEvent && ((MoveEvent)e).destinationPos.Equals(space)) ||
                (e is SpawnEvent && ((SpawnEvent)e).destinationPos.Equals(space))
            );
            block.deniedPos = original is MoveEvent ? ((MoveEvent)original).destinationPos : ((SpawnEvent)original).destinationPos;
            short originalRobotId = original is MoveEvent ? ((MoveEvent)original).robotId : ((SpawnEvent)original).robotId;
            block.robotId = originalRobotId;
            int index = wanted.FindIndex(original);
            int size = wanted.GetLength() - index;
            wanted.RemoveAt(index + 1, size - 1);
            wanted.Add(block);
            if (original is MoveEvent)
            {
                original.success = false;
                DamageEvent d = new DamageEvent();
                d.damage = 1;
                d.remainingHealth = (short)(GetRobot(originalRobotId).health - 1);
                d.robotId = originalRobotId;
                wanted.Add(d);
            }
        });

        // Check for pushing, what the hell is going on here... 
        List<MoveEvent> moveEvents = idsToWantedEvents.ToValueList().Reduce(new List<MoveEvent>(), (ms, evts) => ms.Concat(evts.Filter(e => e is MoveEvent).Map(e => (MoveEvent)e)));
        moveEvents.ForEach(g =>
        {
            Robot occupant = GetRobot(g.destinationPos);
            if (occupant != null)
            {
                bool isAlsoMoving = moveEvents.Any(e => e.robotId == occupant.id && e.success);
                if (!isAlsoMoving && g.success)
                {
                    List<GameEvent> events = idsToWantedEvents.Get(evts => evts.Contains(g));
                    Vector2Int diff = g.destinationPos - g.sourcePos;
                    if (g.Equals(events.Get(0)) && !(events.GetLength() > 1 && events.Get(1) is PushEvent))
                    {
                        PushEvent push = new PushEvent();
                        push.robotId = g.robotId;
                        push.victim = occupant.id;
                        push.direction = diff;
                        events.Add(push);
                        DamageEvent d1 = new DamageEvent();
                        d1.damage = 1;
                        d1.robotId = g.robotId;
                        d1.remainingHealth = (short)(GetRobot(g.robotId).health - 1);
                        events.Add(d1);
                        DamageEvent d2 = new DamageEvent();
                        d2.damage = 1;
                        d2.robotId = occupant.id;
                        d2.remainingHealth = (short)(occupant.health - 1);
                        events.Add(d2);
                        MoveEvent move = new MoveEvent();
                        move.robotId = occupant.id;
                        move.sourcePos = g.destinationPos;
                        move.destinationPos = g.destinationPos + diff;
                        events.Add(move);
                        valid = false;
                    }
                    else
                    {
                        Robot r = GetRobot(g.robotId);
                        int index = events.FindIndex(g);
                        g.success = false;
                        BlockEvent evt = new BlockEvent();
                        evt.deniedPos = g.destinationPos;
                        evt.robotId = g.robotId;
                        evt.blockingObject = occupant.name;
                        events.Add(evt, index + 1);
                        DamageEvent evt2 = new DamageEvent();
                        evt2.damage = 1;
                        evt2.robotId = g.robotId;
                        evt2.remainingHealth = (short)(r.health - 1);
                        events.Add(evt2, index + 2);
                        DamageEvent d2 = new DamageEvent();
                        d2.damage = 1;
                        d2.robotId = occupant.id;
                        d2.remainingHealth = (short)(occupant.health - 1);
                        events.Add(d2);
                        valid = false;
                    }
                }
            }
        });

        //First do swapping positions check
        idsToWantedEvents.ForEach((key1, evts1) =>
        {
            idsToWantedEvents.ForEach((key2, evts2) =>
            {
                Robot r1 = GetRobot(key1);
                Robot r2 = GetRobot(key2);
                MoveEvent m1 = evts1.Find((GameEvent g) => {
                    return g is MoveEvent && ((MoveEvent)g).destinationPos.Equals(r2.position) && g.success;
                }) as MoveEvent;
                MoveEvent m2 = evts2.Find((GameEvent g) => {
                    return g is MoveEvent && ((MoveEvent)g).destinationPos.Equals(r1.position) && g.success;
                }) as MoveEvent;
                if (m1 != null && m2 != null)
                {
                    generateBlockEvent(r2.name, m1.destinationPos)(key1);
                    generateBlockEvent(r1.name, m2.destinationPos)(key2);
                    valid = false;
                }
            });
        });

        //Then do multiple robots contest for one position check
        Dictionary<Vector2Int, List<short>> spaceToIds = new Dictionary<Vector2Int, List<short>>(board.spaces.Length);
        idsToWantedEvents.ForEach((key, evts) =>
        {
            evts.ForEach((GameEvent e) =>
            {
                Vector2Int newspace = Map.NULL_VEC;
                if (e is MoveEvent)
                {
                    newspace = ((MoveEvent)e).destinationPos;
                } else if (e is SpawnEvent)
                {
                    newspace = ((SpawnEvent)e).destinationPos;
                }
                if (!newspace.Equals(Map.NULL_VEC) && e.success)
                {
                    if (spaceToIds.Contains(newspace)) spaceToIds.Get(newspace).Add(key);
                    else spaceToIds.Add(newspace, new List<short>(key));
                }
            });
        });
        spaceToIds.ForEach((space, idsWantSpace) =>
        {
            if (idsWantSpace != null && idsWantSpace.GetLength() > 1)
            {
                valid = false;
                idsWantSpace.ForEach(id => {
                    List<string> otherRobotNames = idsToWantedEvents.ToKeyListFiltered(k => k != id).Map(k => GetRobot(k).name);
                    generateBlockEvent(otherRobotNames.ToString(), space)(id);
                });
            }
        });

        //Then do multiple damage on one robot check
        Dictionary<short, List<DamageEvent>> idsToDamages = new Dictionary<short, List<DamageEvent>>(primary.team.GetLength() + secondary.team.GetLength());
        idsToWantedEvents.ForEachValue(evts =>
        {
            evts.ForEach((GameEvent g) => {
                if (g is DamageEvent) {
                    DamageEvent d = (DamageEvent)g;
                    if (!idsToDamages.Contains(d.robotId))
                    {
                        idsToDamages.Add(d.robotId, new List<DamageEvent>((DamageEvent)g));
                    } else
                    {
                        idsToDamages.Get(d.robotId).Add((DamageEvent)g);
                    }
                }
            });
        });
        idsToDamages.ToValueListFiltered(d => d != null).ForEach(damages =>
        {
            for (int i = 1; i < damages.GetLength(); i++)
            {
                damages.Get(i).remainingHealth = (short)(damages.Get(i - 1).remainingHealth - damages.Get(i).damage);
            }
        });

        return valid;
    }

    private Robot GetRobot(short id)
    {
        return GetRobot(r => r.id == id);
    }

    private Robot GetRobot(Vector2Int space)
    {
        return GetRobot(r => r.position.Equals(space));
    }

    private Robot GetRobot(ReturnAction<Robot, bool> callback)
    {
        return primary.team.Concat(secondary.team).Find(callback);
    }
}
