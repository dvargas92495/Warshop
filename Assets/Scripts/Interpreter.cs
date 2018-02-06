using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Interpreter {

    private static Game.Player[] playerTurnObjectArray;
    private static Map board;

    internal static UIController uiController;
    internal static BoardController boardController;
    internal static RobotController[] robotControllers;
    public static int eventDelay = 1;
    public static string[] myRobotNames = new string[0];
    public static string[] opponentRobotNames = new string[0];
    private static bool myturn;
    private static bool isPrimary;

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

    public static void InitializeUI(UIController ui)
    {
        uiController = ui;
        uiController.InitializeUICanvas(playerTurnObjectArray);
        if (boardController) uiController.PositionCamera(isPrimary);
    }

    public static void InitializeBoard(BoardController bc)
    {
        boardController = bc;
        boardController.InitializeBoard(board);
        InitializeRobots(playerTurnObjectArray);
        if (uiController) uiController.PositionCamera(isPrimary);
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
                r.transform.GetChild(0).GetComponent<Image>().color = (r.isOpponent ? Color.red : Color.blue);
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

    public static void PlayEvents(List<GameEvent> events)
    {
        uiController.StartCoroutine(EventsRoutine(events));
    }

    public static IEnumerator EventsRoutine(List<GameEvent> events)
    {
        byte currentPriority = byte.MaxValue;
        List<GameEvent> eventsThisPriority = new List<GameEvent>();
        for(int i = 0; i < events.Count; i++)
        {
            GameEvent e = events[i];
            if (e.priority < currentPriority)
            {
                foreach (GameEvent evt in eventsThisPriority)
                {
                    RobotController primaryRobot = GetRobot(evt.primaryRobotId);
                    evt.Animate(primaryRobot);
                }
                eventsThisPriority.Clear();
                currentPriority = e.priority;
                i--;
            }
            else
            {
                uiController.DisplayEvent(e.ToString());
                uiController.SetBattery(e.primaryBattery, e.secondaryBattery);
                eventsThisPriority.Add(e);
            }
            yield return new WaitForSeconds(eventDelay);
        }
        uiController.DisplayEvent(GameConstants.FINISHED_EVENTS);
        myturn = true;
        Array.ForEach(robotControllers, (RobotController r) => r.canCommand = !r.isOpponent);
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

    private static RobotController GetRobot(short id)
    {
        return Array.Find(robotControllers, (RobotController r) => r.id == id);
    }
}

