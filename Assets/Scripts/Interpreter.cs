using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Interpreter {

    internal static InitialController initialController;
    internal static UIController uiController;
    internal static BoardController boardController;
    internal static Dictionary<short, RobotController> robotControllers;

    public static string[] myRobotNames = new string[0];
    public static string[] opponentRobotNames = new string[0];

    private const int eventDelay = 1;
    private static bool loadedLocally = false;
    private static Game.Player[] playerTurnObjectArray;
    private static Map board;
    private static Logger log = new Logger(typeof(Interpreter));
    private static bool myturn;
    private static bool isPrimary;
    private static byte turnNumber = 1;
    private static Tuple<byte, byte, byte> currentHistory = new Tuple<byte, byte, byte> ( 1, GameConstants.MAX_PRIORITY, 0 );
    private static byte[] presentState;

    //[turnNumber, priority, commandtype, State]
    private static List<Tuple<byte, byte, byte, byte[]>> History = new List<Tuple<byte, byte, byte, byte[]>>();

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
        if (!loadedLocally) SceneManager.LoadScene("Prototype");
    }

    public static void ClientError(string s)
    {
        initialController.statusText.color = Color.red;
        initialController.statusText.text = s;
    }

    public static void InitializeBoard(BoardController bc)
    {
        boardController = bc;
#if UNITY_EDITOR
        if (board == null && GameConstants.LOCAL_MODE)
        {
            //We are loading from Prototype scene
            loadedLocally = true;
            App.LinkAssets(new TextAsset[] { bc.DefaultBoard });
            myRobotNames = new string[] { "Bronze Grunt", "Silver Grunt", "Bronze Grunt", "Platinum Grunt" };
            opponentRobotNames = new string[] { "Silver Grunt", "Golden Grunt", "Silver Grunt", "Bronze Grunt" };
            ConnectToServer("", "", bc.DefaultBoard.name);
            while (board == null) { }
        }
#endif
        boardController.InitializeBoard(board);
        InitializeRobots(playerTurnObjectArray);
    }

    public static void InitializeUI(UIController ui)
    {
        uiController = ui;
        uiController.InitializeUICanvas(playerTurnObjectArray, isPrimary);
        presentState = SerializeState(-1);
    }

    private static void InitializeRobots(Game.Player[] playerTurns)
    {
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
                r.transform.localPosition = Vector3.right * i + Vector3.back*boardController.tile.transform.localScale.z*1.001f;
                r.transform.rotation = Quaternion.Euler(0, 0, isPrimary ? 0 : 180);
            }

        }
    }

    public static void SubmitActions()
    {
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
            uiController.EachMenuItem(uiController.RobotButtonContainer, (MenuItemController m) =>
            {
                m.gameObject.SetActive(!m.gameObject.activeInHierarchy);
            });
            uiController.SubmitCommands.SetActive(myturn);
        } else
        {
            uiController.SetButtons(false);
            uiController.SetButtons(uiController.RobotButtonContainer, false);
        }
        uiController.SetButtons(uiController.CommandButtonContainer, false);
        uiController.SetButtons(uiController.DirectionButtonContainer, false);
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
            uiController.SetButtons(uiController.RobotButtonContainer, false);
            uiController.SetButtons(uiController.CommandButtonContainer, false);
            uiController.SetButtons(uiController.DirectionButtonContainer, false);
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
        bool gameOver = false;
        int currentUserBattery;
        int currentOpponentBattery;
        int userBattery = uiController.GetUserBattery();
        int opponentBattery = uiController.GetOpponentBattery();
        List<GameEvent> eventsThisPriority = new List<GameEvent>();
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
                currentUserBattery = uiController.GetUserBattery();
                currentOpponentBattery = uiController.GetOpponentBattery();
                uiController.SetBattery(userBattery, opponentBattery);
                History.Add(new Tuple<byte, byte, byte, byte[]>(turnNumber, r.priority, GameEvent.Resolve.GetByte(r.commandType), SerializeState(r.priority)));
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
                uiController.Splash(!((isPrimary && evt.primaryLost) || (!isPrimary && evt.secondaryLost)));
                Array.ForEach(robotControllers.Values.ToArray(), (RobotController r) => r.canCommand = false);
                myturn = false;
                gameOver = true;
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
        if (!gameOver)
        {
            currentHistory = new Tuple<byte, byte, byte> ((byte)(turnNumber + 1), GameConstants.MAX_PRIORITY, 0 );
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
            uiController.SubmitCommands.Activate();
            uiController.BackToPresent.Deactivate();
            uiController.StepForwardButton.Deactivate();
            uiController.StepBackButton.SetActive(History.Count != 0);
            uiController.SetButtons(uiController.RobotButtonContainer, true);
            uiController.LightUpPanel(false, true);
            uiController.LightUpPanel(false, false);
            presentState = SerializeState(-1);
        }
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
        currentHistory = new Tuple<byte, byte, byte>((byte)(turnNumber + 1), GameConstants.MAX_PRIORITY, 0);
        foreach (RobotController r in robotControllers.Values)
        {
            uiController.ClearCommands(r.id);
            r.commands.ForEach((Command c) => uiController.addSubmittedCommand(c, r.id));
            r.canCommand = (!r.isOpponent && !GameConstants.LOCAL_MODE) || (GameConstants.LOCAL_MODE && ((r.isOpponent && !myturn) || (!r.isOpponent && myturn)));
        }
        uiController.SubmitCommands.SetActive(true);
        uiController.SetButtons(uiController.RobotButtonContainer, true);
        uiController.BackToPresent.Deactivate();
        uiController.StepBackButton.Activate();
        uiController.StepForwardButton.Deactivate();
    }

    public static void StepForward()
    {
        Tuple<byte, byte, byte, byte[]> historyState = History.Find((Tuple<byte, byte, byte, byte[]> t) =>
                   (t.Item1 == currentHistory.Item1 && t.Item2 == currentHistory.Item2 && t.Item3 == currentHistory.Item3));
        int historyIndex = History.IndexOf(historyState);
        if (historyIndex == History.Count - 1)
        {
            BackToPresent();
        } else
        {
            Tuple<byte, byte, byte, byte[]> nextState = History[historyIndex + 1];
            GoTo(nextState.Item1, nextState.Item2, nextState.Item3, nextState.Item4);
            uiController.StepForwardButton.Activate();
            uiController.BackToPresent.Activate();
        }
        uiController.StepBackButton.Activate();
    }

    public static void StepBackward()
    {
        int historyIndex = History.Count;
        if (currentHistory.Item1 != turnNumber+1)
        {
            Tuple<byte, byte, byte, byte[]> historyState = History.Find((Tuple<byte, byte, byte, byte[]> t) =>
               (t.Item1 == currentHistory.Item1 && t.Item2 == currentHistory.Item2 && t.Item3 == currentHistory.Item3));
            historyIndex = History.IndexOf(historyState);
        }
        Tuple<byte, byte, byte, byte[]> nextState = History[historyIndex - 1];
        GoTo(nextState.Item1, nextState.Item2, nextState.Item3, nextState.Item4);
        uiController.StepBackButton.SetActive(historyIndex > 1);
        uiController.StepForwardButton.Activate();
        uiController.BackToPresent.Activate();
    }

    private static void GoTo(byte turn, byte priority, byte command, byte[] state)
    {
        DeserializeState(state);
        currentHistory = new Tuple<byte,byte,byte> ( turn, priority, command );
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
        uiController.SubmitCommands.SetActive(false);
        uiController.SetButtons(uiController.RobotButtonContainer, false);
        uiController.SetButtons(uiController.CommandButtonContainer, false);
        uiController.SetButtons(uiController.DirectionButtonContainer, false);
    }

    private static RobotController GetRobot(short id)
    {
        return Array.Find(robotControllers.Values.ToArray(), (RobotController r) => r.id == id);
    }
    
}

