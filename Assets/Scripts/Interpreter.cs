using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;

public class Interpreter {

    private static Game.Player[] playerTurnObjectArray;
    private static Map board;

    internal static InitialController initialController;
    internal static UIController uiController;
    internal static BoardController boardController;
    internal static Dictionary<short, RobotController> robotControllers;
    public const int eventDelay = 1;
    public static string[] myRobotNames = new string[0];
    public static string[] opponentRobotNames = new string[0];
    private static Logger log = new Logger(typeof(Interpreter));
    private static bool myturn;
    private static bool isPrimary;
    private static byte turnNumber = 1;
    private static byte[] currentHistory = new byte[] { 1, GameConstants.MAX_PRIORITY, 0 };
    private static byte[] presentState;

    //[turnNumber][priority][commandtype]
    private static Dictionary<byte, Dictionary<byte, Dictionary<byte, byte[]>>> History = new Dictionary<byte, Dictionary<byte, Dictionary<byte, byte[]>>>();

    public static void ConnectToServer(string playerId, string opponentId, string boardFile)
    {
        if (playerId == "") playerId = "player";
        if (opponentId == "") opponentId = "opponent";
        playerTurnObjectArray = new Game.Player[] {
            new Game.Player(new Robot[0], playerId),
            new Game.Player(new Robot[0], opponentId)
        };
        GameClient.Initialize(playerId, boardFile);
    }

    public static void SendPlayerInfo()
    {
        if (GameConstants.LOCAL_MODE)
        {
            GameClient.SendLocalGameRequest(myRobotNames, opponentRobotNames, playerTurnObjectArray[0].name, playerTurnObjectArray[1].name);
        }
        else
        {
            GameClient.SendGameRequest(myRobotNames, playerTurnObjectArray[0].name);
        }
    }

    public static void LoadBoard(Robot[] myTeam, Robot[] opponentTeam, string opponentName, Map b, bool isP)
    {
        playerTurnObjectArray[0].team = myTeam;
        playerTurnObjectArray[1].team = opponentTeam;
        playerTurnObjectArray[1].name = opponentName;
        board = b;
        myturn = true;
        isPrimary = isP;
        SceneManager.LoadScene("Prototype");
    }

    public static void ClientError(string s)
    {
        initialController.statusText.color = Color.red;
        initialController.statusText.text = s;
    }

    public static void InitializeUI(UIController ui)
    {
        uiController = ui;
        uiController.InitializeUICanvas(playerTurnObjectArray, isPrimary);
        presentState = SerializeState(-1);
    }

    public static void InitializeBoard(BoardController bc)
    {
        boardController = bc;
        boardController.InitializeBoard(board);
        InitializeRobots(playerTurnObjectArray);
    }

    private static void InitializeRobots(Game.Player[] playerTurns)
    {
        boardController.primaryDock.transform.localScale += Vector3.right * (playerTurns[isPrimary ? 0 : 1].team.Length - 1);
        boardController.secondaryDock.transform.localScale += Vector3.right * (playerTurns[isPrimary ? 1 : 0].team.Length - 1);
        boardController.primaryDock.transform.position += Vector3.right * (playerTurns[isPrimary ? 0 : 1].team.Length - 1) / 2;
        boardController.secondaryDock.transform.position += Vector3.left * (playerTurns[isPrimary ? 1 : 0].team.Length - 1) / 2;
        Transform[] docks = isPrimary ? new Transform[] { boardController.primaryDock.transform, boardController.secondaryDock.transform } :
            new Transform[] { boardController.secondaryDock.transform, boardController.primaryDock.transform };
        robotControllers = new Dictionary<short, RobotController>();
        for (int p = 0; p < playerTurns.Length;p++)
        {
            Game.Player player = playerTurns[p];
            Transform dock = docks[p];
            for (int i = 0; i < player.team.Length; i++)
            {
                RobotController r = RobotController.Load(player.team[i], dock);
                r.isOpponent = p == 1;
                r.canCommand = !r.isOpponent;
                r.transform.GetChild(0).GetComponent<SpriteRenderer>().color = (r.isOpponent ? Color.red : Color.blue);
                robotControllers[r.id] = r;
                r.transform.position = dock.position + Vector3.right * (i - dock.localScale.x / 2 + 0.5f);
                r.transform.localScale -= Vector3.right * ((player.team.Length - 1.0f)/player.team.Length) * r.transform.localScale.x;
            }
        }
    }

    public static void SubmitActions()
    {
        DestroyCommandMenu();
        if (!GameConstants.LOCAL_MODE && !myturn)
        {
            return;
        }
        uiController.LightUpPanel(true, true);
        List<Command> commands = new List<Command>();
        string username = (myturn ? playerTurnObjectArray[0].name : playerTurnObjectArray[1].name);
        foreach (RobotController robot in robotControllers.Values)
        {
            if (!robot.canCommand) continue;
            foreach (Command cmd in robot.commands)
            {
                Command c = cmd;
                if (!isPrimary && myturn) c = Util.Flip(c);
                c.robotId = robot.id;
                commands.Add(c);
            }
            robot.canCommand = false;
            uiController.ColorCommandsSubmitted(robot.id);
        }
        if (GameConstants.LOCAL_MODE)
        {
            Array.ForEach(robotControllers.Values.ToArray(), (RobotController r) => r.canCommand = r.isOpponent && myturn);
            uiController.SubmitCommands.interactable = !uiController.SubmitCommands.interactable;
            uiController.OpponentSubmit.interactable = !uiController.OpponentSubmit.interactable;
        } else
        {
            uiController.SetButtons(false);
        }
        myturn = false;
        GameClient.SendSubmitCommands(commands, username);
    }

    public static void DeleteCommand(short rid, int index)
    {
        uiController.ClearCommands(rid);
        RobotController r = GetRobot(rid);
        if (r.commands[index] is Command.Spawn)
        {
            r.commands.Clear();
        }
        else
        {
            r.commands.RemoveAt(index);
            r.commands.ForEach((Command c) => uiController.addSubmittedCommand(c, rid));
        }
    }

    public static void PlayEvents(List<GameEvent> events, byte t)
    {
        turnNumber = t;
        DeserializeState(presentState);
        if (GameConstants.LOCAL_MODE)
        {
            uiController.SetButtons(false);
            uiController.LightUpPanel(true, true);
        }
        foreach (RobotController robot in robotControllers.Values)
        {
            if (GameConstants.LOCAL_MODE || !robot.isOpponent)
            {
                robot.commands.ForEach((Command c) => uiController.addSubmittedCommand(c, robot.id));
                uiController.ColorCommandsSubmitted(robot.id);
            }
        }
        uiController.LightUpPanel(true, false);
        uiController.StartCoroutine(EventsRoutine(events));
    }

    public static IEnumerator EventsRoutine(List<GameEvent> events)
    {
        int currentUserBattery;
        int currentOpponentBattery;
        int userBattery = uiController.GetUserBattery();
        int opponentBattery = uiController.GetOpponentBattery();
        List<GameEvent> eventsThisPriority = new List<GameEvent>();
        Dictionary<byte, Dictionary<byte, byte[]>> priorityToState = new Dictionary<byte, Dictionary<byte, byte[]>>();
        foreach(GameEvent e in events)
        {
            if (!eventsThisPriority.Any())
            {
                userBattery = uiController.GetUserBattery();
                opponentBattery = uiController.GetOpponentBattery();
            }
            if (e is GameEvent.Resolve)
            {
                GameEvent.Resolve r = (GameEvent.Resolve)e;
                uiController.HighlightCommands(r.commandType, r.priority);
                if (!priorityToState.ContainsKey(r.priority)) priorityToState[r.priority] = new Dictionary<byte, byte[]>();
                currentUserBattery = uiController.GetUserBattery();
                currentOpponentBattery = uiController.GetOpponentBattery();
                uiController.SetBattery(userBattery, opponentBattery);
                priorityToState[r.priority][GameEvent.Resolve.GetByte(r.commandType)] = SerializeState((int)r.priority);
                uiController.SetBattery(currentUserBattery, currentOpponentBattery);
                uiController.SetPriority(r.priority);
                foreach (GameEvent evt in eventsThisPriority)
                {
                    RobotController primaryRobot = GetRobot(evt.primaryRobotId);
                    if (evt is GameEvent.Move)
                    {
                        primaryRobot.displayMove(((GameEvent.Move)evt).destinationPos);
                    } else if (evt is GameEvent.Death)
                    {
                        GameEvent.Death d = (GameEvent.Death)evt;
                        primaryRobot.displayHealth(d.returnHealth);
                        primaryRobot.gameObject.SetActive(false);
                        Transform dock = ((primaryRobot.isOpponent && isPrimary) || (!primaryRobot.isOpponent && !isPrimary)) ?
                            boardController.secondaryDock.transform : boardController.primaryDock.transform;
                        primaryRobot.transform.position = dock.position + Vector3.right * (dock.childCount - dock.localScale.x / 2 + 0.5f);
                        primaryRobot.transform.parent = dock;
                    } else if (evt is GameEvent.Spawn)
                    {
                        primaryRobot.transform.parent = boardController.transform;
                        primaryRobot.displayMove(((GameEvent.Spawn)evt).destinationPos);
                    }
                    primaryRobot.clearEvents();
                }
                eventsThisPriority.Clear();
            }
            else if (e is GameEvent.End)
            {
                GameEvent.End evt = (GameEvent.End)e;
                if (evt.primaryLost) boardController.primaryBatteryLocation.GetComponent<SpriteRenderer>().sprite = uiController.GetArrow("Damage");
                if (evt.secondaryLost) boardController.secondaryBatteryLocation.GetComponent<SpriteRenderer>().sprite = uiController.GetArrow("Damage");
                if ((isPrimary && evt.primaryLost) || (!isPrimary && evt.secondaryLost))
                {
                    uiController.SplashScreen.GetComponentInChildren<Text>().text = "YOU LOSE!";
                } else
                {
                    uiController.SplashScreen.GetComponentInChildren<Text>().text = "YOU WIN!";
                }
                uiController.SplashScreen.gameObject.SetActive(true);
                uiController.SetButtons(false);
                Array.ForEach(robotControllers.Values.ToArray(), (RobotController r) => r.canCommand = false);
                myturn = false;
            }
            else
            {
                uiController.SetPriority(e.priority);
                RobotController primaryRobot = GetRobot(e.primaryRobotId);
                if (e is GameEvent.Move)
                {
                    primaryRobot.displayEvent("Move Arrow", ((GameEvent.Move)e).destinationPos);
                }
                else if (e is GameEvent.Attack)
                {
                    Array.ForEach(((GameEvent.Attack)e).locs, (Vector2Int v) => primaryRobot.displayEvent("Attack Arrow", v));
                } else if (e is GameEvent.Block)
                {
                    primaryRobot.displayEvent("Collision", ((GameEvent.Block)e).deniedPos);
                } else if (e is GameEvent.Push)
                {
                    primaryRobot.displayEvent("Push", new Vector2Int((int)primaryRobot.transform.position.x, (int)primaryRobot.transform.position.y) + ((GameEvent.Push)e).direction);
                } else if (e is GameEvent.Damage)
                {
                    primaryRobot.displayEvent("Damage", new Vector2Int((int)primaryRobot.transform.position.x, (int)primaryRobot.transform.position.y));
                    primaryRobot.displayHealth(((GameEvent.Damage)e).remainingHealth);
                }
                else if (e is GameEvent.Miss)
                {
                    Array.ForEach(((GameEvent.Miss)e).locs, (Vector2Int v) => primaryRobot.displayEvent("Missed Attack", v, false));
                }
                else if (e is GameEvent.Battery)
                {
                    Vector3 pos = (((GameEvent.Battery)e).isPrimary ? boardController.primaryBatteryLocation : boardController.secondaryBatteryLocation).transform.position;
                    primaryRobot.displayEvent("Damage", new Vector2Int((int)pos.x, (int)pos.y), false);
                } else if (e is GameEvent.Spawn)
                {
                    primaryRobot.displayEvent("", new Vector2Int(((GameEvent.Spawn)e).destinationPos.x, ((GameEvent.Spawn)e).destinationPos.y), false);
                }
                log.Info(e.ToString());
                uiController.SetBattery(e.primaryBattery, e.secondaryBattery);
                eventsThisPriority.Add(e);
            }
            yield return new WaitForSeconds(eventDelay);
        }
        if (!uiController.SplashScreen.gameObject.activeInHierarchy)
        {
            uiController.priorityArrow.SetActive(false);
            History[turnNumber] = priorityToState;
            currentHistory = new byte[] { (byte)(turnNumber + 1), GameConstants.MAX_PRIORITY, 0 };
            myturn = true;
            Array.ForEach(robotControllers.Values.ToArray(), (RobotController r) =>
            {
                r.canCommand = !r.isOpponent;
                r.gameObject.SetActive(true);
                if (GameConstants.LOCAL_MODE || !r.isOpponent)
                {
                    uiController.ClearCommands(r.id);
                }
                r.commands.Clear();
            });
            uiController.SetButtons(true);
            uiController.LightUpPanel(false, true);
            uiController.LightUpPanel(false, false);
            presentState = SerializeState(-1);
        }
    }

    public static void DestroyCommandMenu()
    {
        foreach (RobotController robot in robotControllers.Values)
        {
            if (robot.menu.activeInHierarchy)
            {
                robot.menu.SetActive(false);
                break;
            }
            if (robot.submenu.activeInHierarchy)
            {
                robot.submenu.SetActive(false);
                break;
            }
        }
        uiController.DestroyCommandMenu();
    }

    private static byte[] SerializeState(int priority)
    {
        BinaryFormatter bf = new BinaryFormatter();
        using (MemoryStream ms = new MemoryStream())
        {
            foreach(RobotController r in robotControllers.Values)
            {
                bf.Serialize(ms, r.id);
                bf.Serialize(ms, r.transform.position.x);
                bf.Serialize(ms, r.transform.position.y);
                bf.Serialize(ms, r.transform.position.z);
                bf.Serialize(ms, r.transform.rotation.eulerAngles.x);
                bf.Serialize(ms, r.transform.rotation.eulerAngles.y);
                bf.Serialize(ms, r.transform.rotation.eulerAngles.z);
                bf.Serialize(ms, r.GetHealth());
                bf.Serialize(ms, r.GetAttack());
                bf.Serialize(ms, r.currentEvents.Count);
                r.currentEvents.ForEach((SpriteRenderer s) =>
                {
                    bf.Serialize(ms, s.transform.position.x);
                    bf.Serialize(ms, s.transform.position.y);
                    bf.Serialize(ms, s.transform.position.z);
                    bf.Serialize(ms, s.transform.rotation.eulerAngles.x);
                    bf.Serialize(ms, s.transform.rotation.eulerAngles.y);
                    bf.Serialize(ms, s.transform.rotation.eulerAngles.z);
                    bf.Serialize(ms, s.sprite.name);
                });
                if (GameConstants.LOCAL_MODE || !r.isOpponent)
                {
                   Tuple<string,byte>[] cmds = uiController.getCommandsSerialized(r.id);
                   bf.Serialize(ms, cmds.Length);
                   Array.ForEach(cmds, (Tuple<string,byte> s) => {
                       bf.Serialize(ms, s.Item1);
                       bf.Serialize(ms, s.Item2);
                   });
                }
            }
            bf.Serialize(ms, uiController.GetUserBattery());
            bf.Serialize(ms, uiController.GetOpponentBattery());
            bf.Serialize(ms, priority);
            return ms.ToArray();
        }
    }

    private static void DeserializeState(byte[] state)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            BinaryFormatter bf = new BinaryFormatter();
            ms.Write(state, 0, state.Length);
            ms.Seek(0, SeekOrigin.Begin);
            for (int i = 0; i < robotControllers.Values.Count; i++)
            {
                short id = (short)bf.Deserialize(ms);
                RobotController r = GetRobot(id);
                float px = (float)bf.Deserialize(ms);
                float py = (float)bf.Deserialize(ms);
                float pz = (float)bf.Deserialize(ms);
                r.transform.position = new Vector3(px, py, pz);
                float rx = (float)bf.Deserialize(ms);
                float ry = (float)bf.Deserialize(ms);
                float rz = (float)bf.Deserialize(ms);
                r.transform.rotation = Quaternion.Euler(rx, ry, rz);
                r.displayHealth((short)bf.Deserialize(ms));
                r.displayAttack((short)bf.Deserialize(ms));
                r.clearEvents();
                int eventCount = (int)bf.Deserialize(ms);
                for (int j = 0; j < eventCount; j++)
                {
                    float spx = (float)bf.Deserialize(ms);
                    float spy = (float)bf.Deserialize(ms);
                    float spz = (float)bf.Deserialize(ms);
                    Vector3 spos = new Vector3(spx, spy, spz);
                    float srx = (float)bf.Deserialize(ms);
                    float sry = (float)bf.Deserialize(ms);
                    float srz = (float)bf.Deserialize(ms);
                    Quaternion srot = Quaternion.Euler(srx, sry, srz);
                    string s = (string)bf.Deserialize(ms);
                    r.displayEvent(s, Vector2Int.zero);
                    r.currentEvents[j].transform.position = spos;
                    r.currentEvents[j].transform.rotation = srot;
                }
                if (GameConstants.LOCAL_MODE || !r.isOpponent)
                {
                    int cmds = (int)bf.Deserialize(ms);
                    uiController.ClearCommands(r.id);
                    for (int j = 0; j < cmds; j++)
                    {
                        string s = (string)bf.Deserialize(ms);
                        byte d = (byte)bf.Deserialize(ms);
                        if (s.StartsWith(Command.Spawn.DISPLAY)) uiController.addSubmittedCommand(new Command.Spawn(d), r.id);
                        if (s.StartsWith(Command.Move.DISPLAY)) uiController.addSubmittedCommand(new Command.Move(d), r.id);
                        if (s.StartsWith(Command.Attack.DISPLAY)) uiController.addSubmittedCommand(new Command.Attack(d), r.id);
                    }
                }
            }
            int userBattery = (int)bf.Deserialize(ms);
            int opponentBattery = (int)bf.Deserialize(ms);
            uiController.SetBattery(userBattery, opponentBattery);
            uiController.SetPriority((int)bf.Deserialize(ms));
        }
    }

    public static void BackToPresent()
    {
        DeserializeState(presentState);
        currentHistory = new byte[] { (byte)(turnNumber + 1), GameConstants.MAX_PRIORITY, 0 };
        foreach (RobotController r in robotControllers.Values)
        {
            uiController.ClearCommands(r.id);
            r.commands.ForEach((Command c) => uiController.addSubmittedCommand(c, r.id));
            r.canCommand = (!r.isOpponent && !GameConstants.LOCAL_MODE) || (GameConstants.LOCAL_MODE && ((r.isOpponent && !myturn) || (!r.isOpponent && myturn)));
        }
        uiController.SubmitCommands.interactable = true;
    }

    public static void StepForward()
    {
        byte turn = currentHistory[0];
        byte priority = currentHistory[1];
        byte command = currentHistory[2];
        bool stepped = false;
        while (turn < turnNumber + 1)
        {
            if (command == 3)
            {
                command = 0;
                if (priority == 0)
                {
                    priority = GameConstants.MAX_PRIORITY;
                    turn++;
                } else
                {
                    priority--;
                }
            } else
            {
                command++;
            }
            if (History.ContainsKey(turn) && History[turn].ContainsKey(priority) && History[turn][priority].ContainsKey(command))
            {
                GoTo(turn, priority, command );
                stepped = true;
                break;
            }
        }
        if (!stepped) {
            BackToPresent();
        }
    }

    public static void StepBackward()
    {
        byte turn = currentHistory[0];
        byte priority = currentHistory[1];
        byte command = currentHistory[2];
        while (!(turn == 1 && priority == GameConstants.MAX_PRIORITY && command == 0))
        {
            if (command == 0)
            {
                command = 3;
                if (priority == GameConstants.MAX_PRIORITY)
                {
                    priority = 0;
                    turn--;
                }
                else
                {
                    priority++;
                }
            }
            else
            {
                command--;
            }
            if (History.ContainsKey(turn) && History[turn].ContainsKey(priority) && History[turn][priority].ContainsKey(command))
            {
                GoTo( turn, priority, command );
                break;
            }
        }
    }

    private static void GoTo(byte turn, byte priority, byte command)
    {
        DeserializeState(History[turn][priority][command]);
        currentHistory = new byte[] { turn, priority, command };
        Array.ForEach(robotControllers.Values.ToArray(), (RobotController r) => {
            uiController.ColorCommandsSubmitted(r.id);
            r.canCommand = false;
        });
        for (byte p = GameConstants.MAX_PRIORITY; p > 0; p--)
        {
            for (byte c = 1; c < 4; c++)
            {
                if (p > priority || (p == priority && c <= command))
                {
                    uiController.HighlightCommands(Command.byteToCmd[c], p);
                }
            }
        }
        uiController.SubmitCommands.interactable = false;
        DestroyCommandMenu();
    }

    private static RobotController GetRobot(short id)
    {
        return Array.Find(robotControllers.Values.ToArray(), (RobotController r) => r.id == id);
    }

    // begin hot key methods

    internal static void SelectRobot(int index)
    {
        int count = 0;
        foreach (RobotController robot in robotControllers.Values)
        {
            if (robot.canCommand) count++;
            if (count == index)
            {
                robot.toggleMenu();
                break;
            }
        }
    }

    internal static void ClickMenuItem(string name)
    {
        foreach (RobotController robot in robotControllers.Values)
        {
            if (robot.menu.activeInHierarchy && robot.menu.transform.Find(name).gameObject.activeInHierarchy)
            {
                robot.toggleSubmenu(name);
                break;
            }
        }
    }

    internal static void ClickSubmenuItem(byte dir)
    {
        foreach (RobotController robot in robotControllers.Values)
        {
            if (robot.submenu.activeInHierarchy)
            {
                string name = robot.submenu.GetComponentInChildren<SpriteRenderer>().sprite.name.Split(' ')[0];
                robot.addRobotCommand(name, dir);
                break;
            }
        }
    }

    // end hot key methods
}

