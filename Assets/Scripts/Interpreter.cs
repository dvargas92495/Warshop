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
    internal static RobotController[] robotControllers;
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
        robotControllers = new RobotController[playerTurns[0].team.Length + playerTurns[1].team.Length];
        for (int p = 0; p < playerTurns.Length;p++)
        {
            Game.Player player = playerTurns[p];
            for (int i = 0; i < player.team.Length; i++)
            {
                RobotController r = RobotController.Load(player.team[i]);
                r.isOpponent = p == 1;
                r.canCommand = !r.isOpponent;
                r.transform.GetChild(0).GetComponent<SpriteRenderer>().color = (r.isOpponent ? Color.red : Color.blue);
                robotControllers[playerTurns[0].team.Length * p + i] = r;
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
        uiController.SetButtons(false);
        uiController.LightUpPanel(true, true);
        List<Command> commands = new List<Command>();
        string username = (myturn ? playerTurnObjectArray[0].name : playerTurnObjectArray[1].name);
        foreach (RobotController robot in robotControllers)
        {
            if (!robot.canCommand) continue;
            foreach (Command cmd in robot.commands)
            {
                Command c = cmd;
                if (!isPrimary || !myturn) c = Util.Flip(c);
                c.robotId = robot.id;
                commands.Add(c);
            }
            uiController.ColorCommandsSubmitted(robot.id);
        }
        if (GameConstants.LOCAL_MODE)
        {
            uiController.Flip();
            Array.ForEach(robotControllers, (RobotController r) => r.canCommand = r.isOpponent && myturn);
        }
        myturn = false;
        GameClient.SendSubmitCommands(commands, username);
    }

    public static void DeleteCommand(short rid, int index)
    {
        RobotController r = GetRobot(rid);
        r.commands.RemoveAt(index);
        r.commands.ForEach((Command c) => uiController.addSubmittedCommand(r.GetArrow(c.ToString()), rid));
    }

    public static void PlayEvents(List<GameEvent> events, byte t)
    {
        turnNumber = t;
        DeserializeState(presentState);
        if (GameConstants.LOCAL_MODE)
        {
            uiController.SetButtons(false);
            uiController.LightUpPanel(true, true);
            foreach (RobotController robot in robotControllers)
            {
                robot.commands.ForEach((Command c) => uiController.addSubmittedCommand(robot.GetArrow(c.ToString()), robot.id));
                uiController.ColorCommandsSubmitted(robot.id);
            }
        }
        uiController.LightUpPanel(true, false);
        uiController.StartCoroutine(EventsRoutine(events));
    }

    public static IEnumerator EventsRoutine(List<GameEvent> events)
    {
        List<GameEvent> eventsThisPriority = new List<GameEvent>();
        Dictionary<byte, Dictionary<byte, byte[]>> priorityToState = new Dictionary<byte, Dictionary<byte, byte[]>>();
        foreach(GameEvent e in events)
        {
            if (e is GameEvent.Resolve)
            {
                GameEvent.Resolve r = (GameEvent.Resolve)e;
                uiController.HighlightCommands(r.commandType, r.priority);
                if (!priorityToState.ContainsKey(r.priority)) priorityToState[r.priority] = new Dictionary<byte, byte[]>();
                priorityToState[r.priority][GameEvent.Resolve.GetByte(r.commandType)] = SerializeState((int)r.priority);
                uiController.SetPriority((int)r.priority);
                foreach (GameEvent evt in eventsThisPriority)
                {
                    RobotController primaryRobot = GetRobot(evt.primaryRobotId);
                    if (evt is GameEvent.Rotate)
                    {
                        primaryRobot.displayRotate(Robot.OrientationToVector(((GameEvent.Rotate)evt).destinationDir));
                    } else if (evt is GameEvent.Move)
                    {
                        primaryRobot.displayMove(((GameEvent.Move)evt).destinationPos);
                    } else if (evt is GameEvent.Death)
                    {
                        GameEvent.Death d = (GameEvent.Death)evt;
                        primaryRobot.displayMove(d.returnLocation);
                        primaryRobot.displayRotate(Robot.OrientationToVector(d.returnDir));
                        primaryRobot.displayHealth(d.returnHealth);
                        primaryRobot.gameObject.SetActive(false);
                    } else if (evt is GameEvent.Damage)
                    {
                        primaryRobot.displayHealth(((GameEvent.Damage)evt).remainingHealth);
                    }
                    primaryRobot.clearEvents();
                }
                eventsThisPriority.Clear();
            }
            else
            {
                uiController.EventTitle.text = "Turn: " + turnNumber;// + " - P " + e.priority;
                uiController.SetPriority((int)e.priority);
                RobotController primaryRobot = GetRobot(e.primaryRobotId);
                if (e is GameEvent.Rotate)
                {
                    GameEvent.Rotate r = (GameEvent.Rotate)e;
                    string label = "Rotate " + Command.Rotate.tostring[r.dir] + " Arrow";
                    Vector2Int facing = new Vector2Int((int)primaryRobot.transform.position.x, (int)primaryRobot.transform.position.y) + Robot.OrientationToVector(r.sourceDir);
                    primaryRobot.displayEvent(label, facing);
                }
                else if (e is GameEvent.Move)
                {
                    primaryRobot.displayEvent("Move Up", ((GameEvent.Move)e).destinationPos);
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
                }
                else if (e is GameEvent.Miss)
                {
                    Array.ForEach(((GameEvent.Miss)e).locs, (Vector2Int v) => primaryRobot.displayEvent("Missed Attack", v, false));
                }
                else if (e is GameEvent.Battery)
                {
                    Vector3 pos = (((GameEvent.Battery)e).isPrimary ? boardController.primaryBatteryLocation : boardController.secondaryBatteryLocation).transform.position;
                    primaryRobot.displayEvent("Damage", new Vector2Int((int)pos.x, (int)pos.y), false);
                }
                log.Info(e.ToString());
                uiController.SetBattery(e.primaryBattery, e.secondaryBattery);
                eventsThisPriority.Add(e);
            }
            yield return new WaitForSeconds(eventDelay);
        }
        uiController.priorityArrow.SetActive(false);
        History[turnNumber] = priorityToState;
        currentHistory = new byte[] { (byte)(turnNumber + 1), GameConstants.MAX_PRIORITY, 0};
        uiController.EventTitle.text = "Turn: " + (byte)(turnNumber + 1);// " - P " + 0;
        myturn = true;
        Array.ForEach(robotControllers, (RobotController r) => {
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

    public static void DestroyCommandMenu()
    {
        foreach (RobotController robot in robotControllers)
        {
            if (robot.menu.activeInHierarchy)
            {
                robot.menu.SetActive(false);
                break;
            }
        }
    }

    private static byte[] SerializeState(int priority)
    {
        BinaryFormatter bf = new BinaryFormatter();
        using (MemoryStream ms = new MemoryStream())
        {
            foreach(RobotController r in robotControllers)
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
                    string[] cmds = uiController.getCommandText(r.id);
                    bf.Serialize(ms, cmds.Length);
                    Array.ForEach(cmds, (string s) => bf.Serialize(ms, s));
                }
            }
            bf.Serialize(ms, uiController.GetUserBattery());
            bf.Serialize(ms, uiController.GetOpponentBattery());
            bf.Serialize(ms, uiController.EventTitle.text);
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
            for (int i = 0; i < robotControllers.Length; i++)
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
                    Sprite[] cmds = new Sprite[(int)bf.Deserialize(ms)];
                    for (int j = 0; j < cmds.Length; j++)
                    {
                        cmds[j] = r.GetArrow((string)bf.Deserialize(ms));
                    }
                    uiController.setCommandText(cmds, r.id);
                }
            }
            int userBattery = (int)bf.Deserialize(ms);
            int opponentBattery = (int)bf.Deserialize(ms);
            uiController.SetBattery(userBattery, opponentBattery);
            uiController.EventTitle.text = (string)bf.Deserialize(ms);
            uiController.SetPriority((int)bf.Deserialize(ms));
        }
    }

    public static void BackToPresent()
    {
        DeserializeState(presentState);
        currentHistory = new byte[] { (byte)(turnNumber + 1), GameConstants.MAX_PRIORITY, 0 };
        foreach (RobotController r in robotControllers)
        {
            uiController.ClearCommands(r.id);
            r.commands.ForEach((Command c) => uiController.addSubmittedCommand(r.GetArrow(c.ToString()), r.id));
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
        Array.ForEach(robotControllers, (RobotController r) => {
            uiController.ColorCommandsSubmitted(r.id);
            r.canCommand = false;
        });
        for (byte p = 0; p < GameConstants.MAX_PRIORITY; p++)
        {
            for (byte c = 0; c < 4; c++)
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
        return Array.Find(robotControllers, (RobotController r) => r.id == id);
    }

    /* Saving this method in case we ever want to format events again
    private static string FormatEvent(string s)
    {
        string[] parts = s.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Equals("Robot"))
            {
                short id = short.Parse(parts[i + 1]);
                RobotController r = GetRobot(id);
                parts[i] = (!r.isOpponent ? "Your" : "Opponent's");
                parts[i + 1] = r.name;
            }
        }
        return string.Join(" ", parts);
    }
    */
}

