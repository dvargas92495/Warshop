using System;
using System.Collections.Generic;
using System.Linq;

namespace WarshopCommon {
    public class Game
    {
        public Player primary = new Player();
        public Player secondary = new Player();
        public string gameSessionId;
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

/*
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
        */

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
            commands.AddRange(primary.FetchCommands());
            commands.AddRange(secondary.FetchCommands());
            Dictionary<short, RobotTurnObject> robotIdToTurnObject = new Dictionary<short, RobotTurnObject>(primary.team.Count + secondary.team.Count);
            List<Robot> allRobots = primary.team.Concat(secondary.team).ToList();
            allRobots.ForEach(r => {
                RobotTurnObject rto = new RobotTurnObject(r.id, r.priority);
                rto.isActive = !r.position.Equals(Map.NULL_VEC);
                robotIdToTurnObject.Add(r.id, rto);
            });
            List<GameEvent> events = new List<GameEvent>();
            for (int p = GameConstants.MAX_PRIORITY; p >= 0; p--)
            {
                HashSet<Command> currentCmds = robotIdToTurnObject.Values.ToList().FindAll(rto => rto.priority == p && commands.Any(c => c.robotId == rto.robotId)).ConvertAll(rto =>
                {
                    rto.priority--;
                    Command cmd = commands.Find(c => c.robotId == rto.robotId);
                    commands.Remove(cmd);
                    return cmd;
                }).ToHashSet();
                List<GameEvent> priorityEvents = new List<GameEvent>();

                currentCmds.ToList().ForEach((Command c) =>
                {
                    Robot primaryRobot = GetRobot(c.robotId);
                    if (!robotIdToTurnObject.GetValueOrDefault(c.robotId).isActive && !(c is Command.Spawn))
                    {
                        currentCmds.Remove(c);
                    }
                });
                currentCmds.ToList().ForEach((Command c) =>
                {
                    Robot primaryRobot = GetRobot(c.robotId);
                    bool isPrimary = c.owner.Equals(primary.name);
                    if (c is Command.Spawn)
                    {
                        priorityEvents.AddRange(primaryRobot.Spawn(board.GetQueuePosition(c.direction, isPrimary), isPrimary));
                    } else if (c is Command.Move)
                    {
                        priorityEvents.AddRange(primaryRobot.Move(c.direction, isPrimary));
                    } else if (c is Command.Attack)
                    {
                        priorityEvents.AddRange(primaryRobot.Attack(c.direction, isPrimary));
                    }
                });

                if (priorityEvents.Count > 0) {
                    ResolveEvent resolveEvent = new ResolveEvent();
                    resolveEvent.robotIdToSpawn = priorityEvents.FindAll(e => e is SpawnEvent)
                                                                .ConvertAll(e => (SpawnEvent)e)
                                                                .ConvertAll(e => new Tuple<short, Tuple<int, int>>(e.robotId, e.destinationPos));
                    resolveEvent.robotIdToMove = priorityEvents.FindAll(e => e is MoveEvent)
                                                            .ConvertAll(e => (MoveEvent)e)
                                                            .ConvertAll(e => new Tuple<short, Tuple<int, int>>(e.robotId, e.destinationPos));
                    resolveEvent.robotIdToHealth = new List<Tuple<short, short>>();
                    resolveEvent.missedAttacks = new List<Tuple<int, int>>();
                    priorityEvents.FindAll(e => e is AttackEvent)
                                .ConvertAll(e => (AttackEvent)e)
                                .ForEach(e => {
                                        Robot attacker = GetRobot(e.robotId);
                                        allRobots.FindAll(robot => e.locs.Contains(robot.position))
                                            .ForEach(r =>
                                            {
                                                short damage = attacker.Damage(r);
                                                Tuple<short, short> robotAndHealth = resolveEvent.robotIdToHealth.Find(t => t.Item1 == r.id);
                                                if(robotAndHealth == null) {
                                                    resolveEvent.robotIdToHealth.Add(new Tuple<short, short>(r.id, (short)(r.health - damage)));
                                                } else {
                                                    resolveEvent.robotIdToHealth.Remove(robotAndHealth);
                                                    robotAndHealth = new Tuple<short, short>(robotAndHealth.Item1, (short)(robotAndHealth.Item2 - damage));
                                                    resolveEvent.robotIdToHealth.Add(robotAndHealth);
                                                }
                                            });
                                        e.locs.FindAll(board.IsBattery).ForEach(v => {
                                            bool isPrimaryBase = board.IsPrimary(v);
                                            short drain = (short)(GameConstants.DEFAULT_BATTERY_MULTIPLIER * attacker.attack);
                                            resolveEvent.primaryBatteryCost += isPrimaryBase ? drain : (short)0;
                                            resolveEvent.secondaryBatteryCost += isPrimaryBase ? (short)0: drain;
                                            resolveEvent.myBatteryHit = resolveEvent.myBatteryHit || isPrimaryBase;
                                            resolveEvent.opponentBatteryHit = resolveEvent.opponentBatteryHit || !isPrimaryBase;
                                        });
                                        e.locs.FindAll(v => !board.IsBattery(v) && !allRobots.Any(r => r.position.Equals(v)) && !board.IsVoid(v))
                                            .ForEach(v => {
                                                if (!resolveEvent.missedAttacks.Contains(v)) resolveEvent.missedAttacks.Add(v);
                                            });
                                        if (e.locs.Any(board.IsVoid)) resolveEvent.robotIdsBlocked.Add(attacker.id);
                                });

                    bool valid = false;
                    while (!valid) {
                        valid = true;

                        // Move x Move
                        List<Tuple<Tuple<int, int>, List<Tuple<short, bool>>>> spacesToRobotIds = new List<Tuple<Tuple<int, int>, List<Tuple<short, bool>>>>();
                        resolveEvent.robotIdToSpawn.ForEach(t => {
                            Tuple<Tuple<int, int>, List<Tuple<short, bool>>> pair = spacesToRobotIds.Find(s => s.Item1.Equals(t.Item2));
                            if (pair == null) {
                                spacesToRobotIds.Add(new Tuple<Tuple<int, int>, List<Tuple<short, bool>>>(t.Item2, new List<Tuple<short, bool>>(){new Tuple<short, bool>(t.Item1, true)}));
                            } else {
                                pair.Item2.Add(new Tuple<short, bool>(t.Item1, true));
                            }
                        });
                        resolveEvent.robotIdToMove.ForEach(t => {
                            Tuple<Tuple<int, int>, List<Tuple<short, bool>>> pair = spacesToRobotIds.Find(s => s.Item1.Equals(t.Item2));
                            if (pair == null) {
                                spacesToRobotIds.Add(new Tuple<Tuple<int, int>, List<Tuple<short, bool>>>(t.Item2, new List<Tuple<short, bool>>(){new Tuple<short, bool>(t.Item1, false)}));
                            } else {
                                pair.Item2.Add(new Tuple<short, bool>(t.Item1, false));
                            }
                        });
                        spacesToRobotIds.FindAll(t => t.Item2.Count > 1).ForEach(t => 
                            t.Item2.ForEach(r => {
                                Tuple<short, Tuple<int, int>> pairToRemove = new Tuple<short, Tuple<int, int>>(r.Item1, t.Item1);
                                if (r.Item2) resolveEvent.robotIdToSpawn.Remove(pairToRemove);
                                else resolveEvent.robotIdToMove.Remove(pairToRemove);
                                resolveEvent.robotIdsBlocked.Add(r.Item1);
                                valid = false;
                            })
                        );

                        // Spawn x Still
                        List<Tuple<short, Tuple<int, int>>> spawnsToBlock = resolveEvent.robotIdToSpawn.FindAll(t => {
                            Robot other = allRobots.Find(r => r.position.Equals(t.Item2));
                            if (other == null) return false;
                            return !resolveEvent.robotIdToMove.Any(m => other.id.Equals(m.Item1));
                        });
                        spawnsToBlock.ForEach(t => {
                            resolveEvent.robotIdToSpawn.Remove(t);
                            resolveEvent.robotIdsBlocked.Add(t.Item1);
                            valid = false;
                        });

                        // Move x Still/Swap
                        List<Tuple<short, Tuple<int, int>>> movesToBlock = resolveEvent.robotIdToMove.FindAll(t => {
                            if(board.IsVoid(t.Item2) || board.IsBattery(t.Item2)) return true;
                            Robot self = allRobots.Find(r => r.id.Equals(t.Item1));
                            Robot other = allRobots.Find(r => r.position.Equals(t.Item2));
                            if (other == null) return false;
                            return !resolveEvent.robotIdToMove.Any(m => other.id.Equals(m.Item1) && !self.position.Equals(m.Item2));
                        });
                        movesToBlock.ForEach(t => {
                            resolveEvent.robotIdToMove.Remove(t);
                            resolveEvent.robotIdsBlocked.Add(t.Item1);
                            valid = false;
                        });
                    }
                    priorityEvents.Add(resolveEvent);

                    List<Tuple<short, short>> delayResolved = resolveEvent.robotIdToHealth.FindAll(h => 
                        resolveEvent.robotIdToMove.Any(m => m.Item1.Equals(h.Item1))
                        || resolveEvent.robotIdsBlocked.Any(b => b.Equals(h.Item1))
                    );
                    if (delayResolved.Count > 0)
                    {
                        delayResolved.ForEach(t => resolveEvent.robotIdToHealth.Remove(t));
                        ResolveEvent delayResolveEvent = new ResolveEvent();
                        delayResolveEvent.robotIdToHealth = delayResolved;
                        priorityEvents.Add(delayResolveEvent);
                    }

                    
                    resolveEvent.robotIdToSpawn.ForEach(t => {
                        GetRobot(t.Item1).position = t.Item2;
                    });
                    resolveEvent.robotIdToMove.ForEach(t => {
                        GetRobot(t.Item1).position = t.Item2;
                    });
                    resolveEvent.robotIdToHealth.ForEach(t => {
                        Robot r = GetRobot(t.Item1);
                        r.health = t.Item2; 
                    });
                }
                
                robotIdToTurnObject.Keys.ToList().ForEach((k) => robotIdToTurnObject.GetValueOrDefault(k).isActive = !GetRobot(k).position.Equals(Map.NULL_VEC));

                priorityEvents.AddRange(processPriorityFinish(primary.team, true));
                priorityEvents.AddRange(processPriorityFinish(secondary.team, false));

                priorityEvents.ForEach(e =>
                {
                    e.priority = (byte)p;
                    primary.battery -= e.primaryBatteryCost;
                    secondary.battery -= e.secondaryBatteryCost;
                });
                events.AddRange(priorityEvents);
                if (primary.battery <= 0 || secondary.battery <= 0)
                {
                    EndEvent e = new EndEvent();
                    e.primaryLost = primary.battery <= 0;
                    e.secondaryLost = secondary.battery <= 0;
                    e.primaryBatteryCost = Math.Max(primary.battery, (short)0);
                    e.secondaryBatteryCost = Math.Max(secondary.battery, (short)0);
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
                    DeathEvent death = new DeathEvent();
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
            return primary.team.Concat(secondary.team).ToList().Find(r => r.id == id);
        }
    }

}
