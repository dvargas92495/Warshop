using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class BaseGameManager
{
    private static BaseGameManager instance;

    protected BoardController boardController;
    protected SetupController setupController;
    protected Game.Player myPlayer;
    protected Game.Player opponentPlayer;
    protected GameClient gameClient;

    // region deprecate
    internal static UIController uiController;
    internal static Dictionary<short, RobotController> robotControllers;
    internal static string ErrorString = "";
    internal static bool gameOver;

    private static Map board;
    private static Logger log = new Logger(typeof(BaseGameManager));
    private static bool myturn;
    private static byte turnNumber = 1;
    private static Tuple<byte, byte, byte> currentHistory = new Tuple<byte, byte, byte>(1, GameConstants.MAX_PRIORITY, 0);
    private static byte[] presentState;

    // [turnNumber, priority, commandtype, State]
    private static List<Tuple<byte, byte, byte, byte[]>> History = new List<Tuple<byte, byte, byte, byte[]>>();
    // endregion deprecate

    internal static void InitializeLocal()
    {
        instance = new LocalGameManager();
    }

    internal static void InitializeStandard(string playerSessionId, string ipAddress, int port)
    {
        instance = new StandardGameManager(playerSessionId, ipAddress, port);
    }

    internal static void InitializeSetup(SetupController sc)
    {
        instance.InitializeSetupImpl(sc);
    }

    protected void InitializeSetupImpl(SetupController sc)
    {
        setupController = sc;
    }

    public static void SendPlayerInfo(string[] myRobotNames, string username)
    {
        instance.SendPlayerInfoImpl(myRobotNames, username);
    }

    protected void SendPlayerInfoImpl(string[] myRobotNames, string username)
    {
        gameOver = false;
        myPlayer = new Game.Player(new Robot[0], username);
    }

    internal void LoadBoard(Robot[] myTeam, Robot[] opponentTeam, string opponentName, Map b)
    {
        myPlayer.team = myTeam;
        opponentPlayer.team = opponentTeam;
        opponentPlayer.name = opponentName;
        board = b;
        myturn = true;
        SceneManager.LoadScene("Match");
    }

    public static void InitializeBoard(BoardController bc)
    {
        instance.InitializeBoardImpl(bc);
    }

    public void InitializeBoardImpl(BoardController bc)
    {
        boardController = bc;
        boardController.InitializeBoard(board);
        boardController.SetBattery(myPlayer.battery, opponentPlayer.battery);
        InitializeRobots();
    }

    public static void InitializeUI(UIController ui)
    {
        instance.InitializeUiImpl(ui);
    }

    public void InitializeUiImpl(UIController ui)
    {
        uiController = ui;
        uiController.InitializeUICanvas(myPlayer, opponentPlayer);
        uiController.SubmitCommands.SetCallback(SubmitActions);
        uiController.BackToPresent.SetCallback(BackToPresent);
        uiController.StepForwardButton.SetCallback(StepForward);
        uiController.StepBackButton.SetCallback(StepBackward);
        presentState = SerializeState(-1);
    }

    private void InitializeRobots()
    {
        robotControllers = new Dictionary<short, RobotController>();
        InitializePlayerRobots(myPlayer, boardController.myDock);
        InitializePlayerRobots(opponentPlayer, boardController.opponentDock);
    }

    private void InitializePlayerRobots(Game.Player player, DockController dock)
    {
        Util.ForEach(player.team, r => InitializeRobot(r, dock));
    }

    private void InitializeRobot(Robot robot, DockController dock)
    {
        RobotController r = boardController.LoadRobot(robot, dock.transform);
        r.isOpponent = dock.Equals(boardController.opponentDock);
        r.canCommand = !r.isOpponent;
        robotControllers[r.id] = r;
        r.transform.localPosition = dock.PlaceInBelt();
    }

    public void SubmitActions()
    {
        if (!GameConstants.LOCAL_MODE && !myturn)
        {
            return;
        }
        uiController.LightUpPanel(true, true);
        List<Command> commands = new List<Command>();
        string username = (myturn ? myPlayer.name : opponentPlayer.name);
        foreach (RobotController robot in robotControllers.Values)
        {
            if (!robot.canCommand) continue;
            foreach (Command cmd in robot.commands)
            {
                Command c = cmd;
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
        } else
        {
            uiController.SetButtons(false);
            uiController.SetButtons(uiController.RobotButtonContainer, false);
        }
        robotControllers.Values.ToList().ForEach((RobotController otherR) => Util.ChangeLayer(otherR.gameObject, uiController.BoardLayer));
        uiController.SetButtons(uiController.CommandButtonContainer, false);
        uiController.SetButtons(uiController.DirectionButtonContainer, false); ;
        uiController.SubmitCommands.Deactivate();
        myturn = false;
        gameClient.SendSubmitCommands(commands.ToArray(), username);
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

    public void PlayEvents(List<GameEvent> events, byte t)
    {
        turnNumber = t;
        if (GameConstants.LOCAL_MODE)
        {
            uiController.SetButtons(false);
            uiController.SetButtons(uiController.RobotButtonContainer, false);
            uiController.SetButtons(uiController.CommandButtonContainer, false);
            uiController.SetButtons(uiController.DirectionButtonContainer, false);
            uiController.LightUpPanel(true, true);
        }
        uiController.LightUpPanel(true, false);
        NextEvent(events, new InfoThisPriority(), 0);
    }

    private class InfoThisPriority {
        internal List<GameEvent> events = new List<GameEvent>();
        internal int userBattery;
        internal int opponentBattery;
        internal int animationsToPlay = 0;
    }

    private void NextEvent(List<GameEvent> events, InfoThisPriority infoThisPriority, int i)
    {
        if (i >= events.Count)
        {
            if (!gameOver)
            {
                currentHistory = new Tuple<byte, byte, byte>((byte)(turnNumber + 1), GameConstants.MAX_PRIORITY, 0);
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
                uiController.SubmitCommands.Deactivate();
                uiController.BackToPresent.Deactivate();
                uiController.StepForwardButton.Deactivate();
                uiController.StepBackButton.SetActive(History.Count != 0);
                uiController.SetButtons(uiController.RobotButtonContainer, true);
                uiController.LightUpPanel(false, true);
                uiController.LightUpPanel(false, false);
                presentState = SerializeState(-1);
            }
            else
            {
                gameClient.SendEndGameRequest();
            }
            return;
        }
        GameEvent e = events[i];
        if (!infoThisPriority.events.Any())
        {
            infoThisPriority.userBattery = boardController.GetMyBatteryScore();
            infoThisPriority.opponentBattery = boardController.GetOpponentBatteryScore();
        }
        Action<int> Next = (int j) => NextEvent(events, infoThisPriority, j); 
        if (e is GameEvent.Resolve)
        {
            GameEvent.Resolve r = (GameEvent.Resolve)e;
            uiController.HighlightCommands(r.commandType, r.priority);
            int currentUserBattery = boardController.GetMyBatteryScore();
            int currentOpponentBattery = boardController.GetOpponentBatteryScore();
            boardController.SetBattery(infoThisPriority.userBattery, infoThisPriority.opponentBattery);
            History.Add(new Tuple<byte, byte, byte, byte[]>(turnNumber, r.priority, GameEvent.Resolve.GetByte(r.commandType), SerializeState(r.priority)));
            boardController.SetBattery(currentUserBattery, currentOpponentBattery);
            uiController.SetPriority(r.priority);
            Action callback = () =>
            {
                if (--infoThisPriority.animationsToPlay == 0) Next(i + 1);
            };
            foreach (GameEvent evt in infoThisPriority.events)
            {
                if (!evt.success) continue;
                RobotController primaryRobot = GetRobot(evt.primaryRobotId);
                infoThisPriority.animationsToPlay++;
                if (evt is GameEvent.Move) primaryRobot.displayMove(((GameEvent.Move)evt).destinationPos, callback, boardController.PlaceRobot);
                else if (evt is GameEvent.Attack) primaryRobot.displayAttack(((GameEvent.Attack)evt).locs[0], callback);
                else if (evt is GameEvent.Death) primaryRobot.displayDeath(((GameEvent.Death)evt).returnHealth, callback, () => {
                    boardController.UnplaceRobot(primaryRobot);
                    DockController dock = !primaryRobot.isOpponent ? boardController.myDock : boardController.opponentDock;
                    primaryRobot.transform.parent = dock.transform;
                    primaryRobot.transform.localPosition = dock.PlaceInBelt();
                });
                else if (evt is GameEvent.Spawn) primaryRobot.displaySpawn(((GameEvent.Spawn)evt).destinationPos, callback, () => {
                    (primaryRobot.isOpponent ? boardController.opponentDock : boardController.myDock).RemoveFromBelt(primaryRobot.transform.localPosition);
                    primaryRobot.transform.parent = boardController.transform;
                    boardController.PlaceRobot(primaryRobot, ((GameEvent.Spawn)evt).destinationPos.x, ((GameEvent.Spawn)evt).destinationPos.y);
                });
                else infoThisPriority.animationsToPlay--;
                primaryRobot.clearEvents();
            }
            infoThisPriority.events.Clear();
            if (infoThisPriority.animationsToPlay == 0) Next(i + 1);
        }
        else if (e is GameEvent.End)
        {
            GameEvent.End evt = (GameEvent.End)e;
            if (evt.primaryLost) boardController.GetMyBattery().transform.Rotate(Vector3.down * 90);
            if (evt.secondaryLost) boardController.GetOpponentBattery().transform.Rotate(Vector3.down * 90);
            uiController.Splash(evt.primaryLost);
            Array.ForEach(robotControllers.Values.ToArray(), (RobotController r) => r.canCommand = false);
            uiController.statsInterface.Initialize(evt, true);
            myturn = false;
            gameOver = true;
            Next(i + 1);
        }
        else
        {
            uiController.SetPriority(e.priority);
            RobotController primaryRobot = GetRobot(e.primaryRobotId);
            UnityAction callback = () => Next(i + 1);
            bool goNext = false;
            if (e is GameEvent.Move) primaryRobot.displayEvent("Move Arrow", ((GameEvent.Move)e).destinationPos, callback);
            else if (e is GameEvent.Attack) Array.ForEach(((GameEvent.Attack)e).locs, (Vector2Int v) => primaryRobot.displayEvent("Attack Arrow", v, callback));
            else if (e is GameEvent.Block) primaryRobot.displayEvent("Collision", ((GameEvent.Block)e).deniedPos, callback);
            else if (e is GameEvent.Push) primaryRobot.displayEvent("Push", new Vector2Int((int)primaryRobot.transform.position.x, (int)primaryRobot.transform.position.y) + ((GameEvent.Push)e).direction, callback);
            else if (e is GameEvent.Damage)
            {
                primaryRobot.displayEvent("Damage", new Vector2Int((int)primaryRobot.transform.position.x, (int)primaryRobot.transform.position.y), callback);
                primaryRobot.displayHealth(((GameEvent.Damage)e).remainingHealth);
            }
            else if (e is GameEvent.Miss) Array.ForEach(((GameEvent.Miss)e).locs, (Vector2Int v) => primaryRobot.displayEvent("Missed Attack", v, callback, false));
            else if (e is GameEvent.Battery)
            {
                Vector3 pos = (((GameEvent.Battery)e).isPrimary ? boardController.GetMyBattery(): boardController.GetOpponentBattery()).transform.position;
                primaryRobot.displayEvent("Damage", new Vector2Int((int)pos.x, (int)pos.y), callback, false);
            }
            else if (e is GameEvent.Spawn) primaryRobot.displayEvent("", new Vector2Int(((GameEvent.Spawn)e).destinationPos.x, ((GameEvent.Spawn)e).destinationPos.y), callback, false);
            else goNext = true;
            log.Info(e.ToString());
            boardController.SetBattery(e.primaryBattery, e.secondaryBattery);
            infoThisPriority.events.Add(e);
            if (goNext) callback();
        }
    }

    private byte[] SerializeState(int priority)
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
            foreach (TileController t in boardController.GetComponentsInChildren<TileController>())
            {
                Material m = t.GetComponent<MeshRenderer>().material;
                if (m.name.StartsWith(t.baseTile.name)) bf.Serialize(ms, 0);
                else if (m.name.StartsWith(t.userBaseTile.name)) bf.Serialize(ms, 1);
                else if (m.name.StartsWith(t.opponentBaseTile.name)) bf.Serialize(ms, 2);
                else throw new Exception("Unknown material: " + m);
            }
            bf.Serialize(ms, boardController.GetMyBatteryScore());
            bf.Serialize(ms, boardController.GetOpponentBatteryScore());
            bf.Serialize(ms, priority);
            return ms.ToArray();
        }
    }

    private void DeserializeState(byte[] state)
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
            foreach (TileController t in boardController.GetComponentsInChildren<TileController>())
            {
                int m = (int)bf.Deserialize(ms);
                if (m == 0) t.GetComponent<MeshRenderer>().material = boardController.tile.baseTile;
                else if (m == 1) t.GetComponent<MeshRenderer>().material = boardController.tile.userBaseTile;
                else if (m == 2) t.GetComponent<MeshRenderer>().material = boardController.tile.opponentBaseTile;
            }
            int userBattery = (int)bf.Deserialize(ms);
            int opponentBattery = (int)bf.Deserialize(ms);
            boardController.SetBattery(userBattery, opponentBattery);
            uiController.SetPriority((int)bf.Deserialize(ms));
        }
    }

    public void BackToPresent()
    {
        DeserializeState(presentState);
        currentHistory = new Tuple<byte, byte, byte>((byte)(turnNumber + 1), GameConstants.MAX_PRIORITY, 0);
        foreach (RobotController r in robotControllers.Values)
        {
            uiController.ClearCommands(r.id);
            r.commands.ForEach((Command c) => uiController.addSubmittedCommand(c, r.id));
            r.canCommand = (!r.isOpponent && !GameConstants.LOCAL_MODE) || (GameConstants.LOCAL_MODE && ((r.isOpponent && !myturn) || (!r.isOpponent && myturn)));
        }
        uiController.SetButtons(uiController.RobotButtonContainer, true);
        uiController.SubmitCommands.SetActive(robotControllers.Values.Any((RobotController r) => r.commands.Count > 0));
        uiController.BackToPresent.Deactivate();
        uiController.StepBackButton.Activate();
        uiController.StepForwardButton.Deactivate();
    }

    public void StepForward()
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

    public void StepBackward()
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

    private void GoTo(byte turn, byte priority, byte command, byte[] state)
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
        uiController.SubmitCommands.Deactivate();
        robotControllers.Values.ToList().ForEach((RobotController otherR) => Util.ChangeLayer(otherR.gameObject, uiController.BoardLayer));
        uiController.SetButtons(uiController.RobotButtonContainer, false);
        uiController.SetButtons(uiController.CommandButtonContainer, false);
        uiController.SetButtons(uiController.DirectionButtonContainer, false);
    }

    private static RobotController GetRobot(short id)
    {
        return Array.Find(robotControllers.Values.ToArray(), (RobotController r) => r.id == id);
    }
    
}

