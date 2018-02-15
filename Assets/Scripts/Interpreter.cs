using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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
    private static bool myturn;
    private static bool isPrimary;
    private static int turnNumber = 1;
    private static List<Dictionary<byte, byte[]>> History = new List<Dictionary<byte, byte[]>>();

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
        uiController.InitializeUICanvas(playerTurnObjectArray);
        uiController.PositionCamera(isPrimary);
        History.Add(new Dictionary<byte, byte[]> { { 0, SerializeState() } });
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
        if (GameConstants.LOCAL_MODE)
        {
            uiController.Flip();
        }
        if (!GameConstants.LOCAL_MODE && !myturn)
        {
            return;
        } else if (GameConstants.LOCAL_MODE && myturn)
        {
            Array.ForEach(robotControllers, (RobotController r) => r.canCommand = r.isOpponent);
        } else
        {
            Array.ForEach(robotControllers, (RobotController r) => r.canCommand = false);
        }
        uiController.DisplayEvent(GameConstants.IM_WAITING);
        List<Command> commands = new List<Command>();
        string username = (myturn ? playerTurnObjectArray[0].name : playerTurnObjectArray[1].name);
        foreach (RobotController robot in robotControllers)
        {
            List<Command> robotCommands = robot.commands;
            foreach (Command cmd in robotCommands)
            {
                Command c = cmd;
                if (!isPrimary || !myturn) c = Util.Flip(c);
                c.robotId = robot.id;
                commands.Add(c);
            }
            robot.commands.Clear();
        }
        myturn = false;
        GameClient.SendSubmitCommands(commands, username);
    }

    public static void DeleteCommand(short rid, int index)
    {
        RobotController r = GetRobot(rid);
        r.commands.RemoveAt(index);
        r.commands.ForEach((Command c) => uiController.addSubmittedCommand(c, rid));
    }

    public static void PlayEvents(List<GameEvent> events, int t)
    {
        DeserializeState(History[History.Count - 1][0]);
        turnNumber = t;
        uiController.StartEventModal(turnNumber, GameConstants.MAX_PRIORITY);
        uiController.StartCoroutine(EventsRoutine(events));
    }

    public static IEnumerator EventsRoutine(List<GameEvent> events)
    {
        byte currentPriority = GameConstants.MAX_PRIORITY;
        List<GameEvent> eventsThisPriority = new List<GameEvent>();
        Dictionary<byte, byte[]> priorityToState = new Dictionary<byte, byte[]>();
        for(int i = 0; i <= events.Count; i++)
        {
            if (i == events.Count || events[i].priority < currentPriority)
            {
                foreach (GameEvent evt in eventsThisPriority)
                {
                    RobotController primaryRobot = GetRobot(evt.primaryRobotId);
                    evt.Animate(primaryRobot);
                    primaryRobot.clearEvents();
                }
                eventsThisPriority.Clear();
                priorityToState[currentPriority] = SerializeState();
                currentPriority--;
                if (currentPriority != byte.MaxValue)
                {
                    uiController.StartEventModal(turnNumber, currentPriority);
                    i--;
                }
            }
            else
            {
                events[i].DisplayEvent(GetRobot(events[i].primaryRobotId));
                uiController.DisplayEvent(FormatEvent(events[i].ToString()));
                uiController.SetBattery(events[i].primaryBattery, events[i].secondaryBattery);
                eventsThisPriority.Add(events[i]);
            }
            yield return new WaitForSeconds(eventDelay);
        }
        uiController.DisplayEvent(GameConstants.FINISHED_EVENTS);
        History.Add(priorityToState);
        myturn = true;
        Array.ForEach(robotControllers, (RobotController r) => {
            r.canCommand = !r.isOpponent;
            r.gameObject.SetActive(true);
        });
    }

    public static void DestroyCommandMenu()
    {
        foreach (RobotController robot in robotControllers)
        {
            if (robot.isMenuShown)
            {
                robot.isMenuShown = false;
                robot.displayHideMenu(false);
                break;
            }
        }
    }

    private static byte[] SerializeState()
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
                bf.Serialize(ms, uiController.GetHealth(r.id));
                bf.Serialize(ms, uiController.GetAttack(r.id));
            }
            bf.Serialize(ms, uiController.GetUserBattery());
            bf.Serialize(ms, uiController.GetOpponentBattery());
            bf.Serialize(ms, uiController.EventTitle.text);
            bf.Serialize(ms, uiController.EventLog.text);
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
                uiController.UpdateHealth(r.id, (short)bf.Deserialize(ms));
                uiController.UpdateAttack(r.id, (short)bf.Deserialize(ms));
            }
            int userBattery = (int)bf.Deserialize(ms);
            int opponentBattery = (int)bf.Deserialize(ms);
            uiController.SetBattery(userBattery, opponentBattery);
            uiController.EventTitle.text = (string)bf.Deserialize(ms);
            uiController.EventLog.text = (string)bf.Deserialize(ms);
        }
    }

    public static void StepBackward()
    {
        short p = short.Parse(uiController.EventTitle.text.Substring(uiController.EventTitle.text.Length - 1));
        short newP = (short)(p + 1);
        if (turnNumber == 0 && newP > 0) return;
        if (newP > GameConstants.MAX_PRIORITY)
        {
            newP = 0;
            turnNumber--;
        }
        DeserializeState(History[turnNumber][(byte)newP]);
    }

    public static void StepForward()
    {
        short p = short.Parse(uiController.EventTitle.text.Substring(uiController.EventTitle.text.Length - 1));
        if (turnNumber == History.Count - 1 && p == 0) return;
        short newP = (short)(p - 1);
        if (newP < 0)
        {
            newP = GameConstants.MAX_PRIORITY;
            turnNumber++;
        }
        DeserializeState(History[turnNumber][(byte)newP]);
    }

    private static RobotController GetRobot(short id)
    {
        return Array.Find(robotControllers, (RobotController r) => r.id == id);
    }

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
}

