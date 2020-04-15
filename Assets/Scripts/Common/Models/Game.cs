using UnityEngine;
using UnityEngine.Events;

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
        internal bool ready;
        internal bool joined;
        internal List<Command> commands;
        internal int connectionId;
        internal Player(){}
        internal Player(Robot[] t, string n)
        {
            team = new List<Robot>(t);
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

            if (priorityEvents.GetLength() > 0) {
                ResolveEvent resolveEvent = new ResolveEvent();
                resolveEvent.robotIdToSpawn = priorityEvents.Filter(e => e is SpawnEvent)
                                                            .Map(e => (SpawnEvent)e)
                                                            .Map(e => new Tuple<short, Vector2Int>(e.robotId, e.destinationPos));
                resolveEvent.robotIdToMove = priorityEvents.Filter(e => e is MoveEvent)
                                                        .Map(e => (MoveEvent)e)
                                                        .Map(e => new Tuple<short, Vector2Int>(e.robotId, e.destinationPos));
                resolveEvent.robotIdToHealth = new List<Tuple<short, short>>();
                resolveEvent.missedAttacks = new List<Vector2Int>();
                priorityEvents.Filter(e => e is AttackEvent)
                            .Map(e => (AttackEvent)e)
                            .ForEach(e => {
                                    Robot attacker = GetRobot(e.robotId);
                                    allRobots.Filter(robot => e.locs.Contains(robot.position))
                                        .ForEach(r =>
                                        {
                                            short damage = attacker.Damage(r);
                                            Tuple<short, short> robotAndHealth = resolveEvent.robotIdToHealth.Find(t => t.GetLeft() == r.id);
                                            if(robotAndHealth == null) {
                                                resolveEvent.robotIdToHealth.Add(new Tuple<short, short>(r.id, (short)(r.health - damage)));
                                            } else {
                                                robotAndHealth.SetRight((short)(robotAndHealth.GetRight() - damage));
                                            }
                                        });
                                    e.locs.Filter(board.IsBattery).ForEach(v => {
                                        bool isPrimaryBase = board.IsPrimary(v);
                                        short drain = (short)(GameConstants.DEFAULT_BATTERY_MULTIPLIER * attacker.attack);
                                        resolveEvent.primaryBatteryCost += isPrimaryBase ? drain : (short)0;
                                        resolveEvent.secondaryBatteryCost += isPrimaryBase ? (short)0: drain;
                                        resolveEvent.myBatteryHit = resolveEvent.myBatteryHit || isPrimaryBase;
                                        resolveEvent.opponentBatteryHit = resolveEvent.opponentBatteryHit || !isPrimaryBase;
                                    });
                                    e.locs.Filter(v => !board.IsBattery(v) && !allRobots.Any(r => r.position.Equals(v)))
                                          .ForEach(v => {
                                              if (!resolveEvent.missedAttacks.Contains(v)) resolveEvent.missedAttacks.Add(v);
                                          });
                            });

                bool valid = false;
                while (!valid) {
                    valid = true;

                    // Move x Move
                    List<Tuple<Vector2Int, List<Tuple<short, bool>>>> spacesToRobotIds = new List<Tuple<Vector2Int, List<Tuple<short, bool>>>>();
                    resolveEvent.robotIdToSpawn.ForEach(t => {
                        Tuple<Vector2Int, List<Tuple<short, bool>>> pair = spacesToRobotIds.Find(s => s.GetLeft().Equals(t.GetRight()));
                        if (pair == null) {
                            spacesToRobotIds.Add(new Tuple<Vector2Int, List<Tuple<short, bool>>>(t.GetRight(), new List<Tuple<short, bool>>(new Tuple<short, bool>(t.GetLeft(), true))));
                        } else {
                            pair.GetRight().Add(new Tuple<short, bool>(t.GetLeft(), true));
                        }
                    });
                    resolveEvent.robotIdToMove.ForEach(t => {
                        Tuple<Vector2Int, List<Tuple<short, bool>>> pair = spacesToRobotIds.Find(s => s.GetLeft().Equals(t.GetRight()));
                        if (pair == null) {
                            spacesToRobotIds.Add(new Tuple<Vector2Int, List<Tuple<short, bool>>>(t.GetRight(), new List<Tuple<short, bool>>(new Tuple<short, bool>(t.GetLeft(), false))));
                        } else {
                            pair.GetRight().Add(new Tuple<short, bool>(t.GetLeft(), false));
                        }
                    });
                    spacesToRobotIds.Filter(t => t.GetRight().GetLength() > 1).ForEach(t => 
                        t.GetRight().ForEach(r => {
                            Tuple<short, Vector2Int> pairToRemove = new Tuple<short, Vector2Int>(r.GetLeft(), t.GetLeft());
                            if (r.GetRight()) resolveEvent.robotIdToSpawn.Remove(pairToRemove);
                            else resolveEvent.robotIdToMove.Remove(pairToRemove);
                            resolveEvent.robotIdsBlocked.Add(r.GetLeft());
                            valid = false;
                        })
                    );

                    // Spawn x Still
                    List<Tuple<short, Vector2Int>> spawnsToBlock = resolveEvent.robotIdToSpawn.Filter(t => {
                        Robot other = allRobots.Find(r => r.position.Equals(t.GetRight()));
                        if (other == null) return false;
                        return !resolveEvent.robotIdToMove.Any(m => other.id.Equals(m.GetLeft()));
                    });
                    spawnsToBlock.ForEach(t => {
                        resolveEvent.robotIdToSpawn.Remove(t);
                        resolveEvent.robotIdsBlocked.Add(t.GetLeft());
                        valid = false;
                    });

                    // Move x Still/Swap
                    List<Tuple<short, Vector2Int>> movesToBlock = resolveEvent.robotIdToMove.Filter(t => {
                        if(board.IsVoid(t.GetRight()) || board.IsBattery(t.GetRight())) return true;
                        Robot self = allRobots.Find(r => r.id.Equals(t.GetLeft()));
                        Robot other = allRobots.Find(r => r.position.Equals(t.GetRight()));
                        if (other == null) return false;
                        return !resolveEvent.robotIdToMove.Any(m => other.id.Equals(m.GetLeft()) && !self.position.Equals(m.GetRight()));
                    });
                    movesToBlock.ForEach(t => {
                        resolveEvent.robotIdToMove.Remove(t);
                        resolveEvent.robotIdsBlocked.Add(t.GetLeft());
                        valid = false;
                    });
                }

                priorityEvents.Add(resolveEvent);
                
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
                evts.Add(death);
            }
        });
        return evts;
    }

    private Robot GetRobot(short id)
    {
        return GetRobot(r => r.id == id);
    }

    private Robot GetRobot(ReturnAction<Robot, bool> callback)
    {
        return primary.team.Concat(secondary.team).Find(callback);
    }
}
