using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class Interpreter : MonoBehaviour {

    private static Game.Player[] playerTurnObjectArray;
    private static Map board;

    internal static UIController uiController;
    internal static BoardController boardController;
    internal static RobotController[] robotControllers;
    public static int eventDelay = 1;
    public static string boardFile = GameConstants.PROTOBOARD_FILE;
    public static string[] myRobotNames = new string[0];
    public static string[] opponentRobotNames = new string[0];
    private static bool myturn;

    public static void ConnectToServer()
    {
        GameClient.Initialize();
    }

    public static void SendPlayerInfo()
    {
        if (GameConstants.LOCAL_MODE)
        {
            GameClient.SendLocalGameRequest(myRobotNames, opponentRobotNames, boardFile);
        }
        else
        {
            GameClient.SendGameRequest(myRobotNames, boardFile);
        }
    }

    public static void LoadBoard(Game.Player[] ptos, Map b)
    {
        playerTurnObjectArray = ptos;
        board = b;
        myturn = true;
        SceneManager.LoadScene("Prototype");
    }

    public static void InitializeUI(UIController ui)
    {
        uiController = ui;
        uiController.InitializeUICanvas(playerTurnObjectArray);
    }

    public static void InitializeBoard(BoardController bc)
    {
        boardController = bc;
        boardController.InitializeBoard(board);
        InitializeRobots(playerTurnObjectArray);
    }

    private static void InitializeRobots(Game.Player[] playerTurns)
    {
        int playerCount = 0;
        int robotCount = 0;
        int p1count = 0; //hack
        robotControllers = new RobotController[playerTurns[0].team.Length + playerTurns[1].team.Length];
        foreach (Game.Player player in playerTurns)
        {
            foreach(Robot robot in player.team)
            {
                RobotController r = RobotController.Make(robot);
                r.isOpponent = playerCount == 1;
                r.canCommand = !r.isOpponent;
                robotControllers[p1count + robotCount] = r;
                robotCount++;
            }
            p1count = robotCount;
            robotCount = 0;
            playerCount++;
        }
    }

    public static void SubmitActions()
    {
        DestroyCommandMenu();
        if (!GameConstants.LOCAL_MODE && !myturn)
        {
            return;
        } else if (GameConstants.LOCAL_MODE && !myturn)
        {
            Array.ForEach(robotControllers, (RobotController r) => r.canCommand = false);
        } else
        {
            Array.ForEach(robotControllers, (RobotController r) => r.canCommand = r.isOpponent);
        }
        List<Command> commands = new List<Command>();
        RobotController[] robots = FindObjectsOfType<RobotController>();
        foreach(RobotController robot in robots)
        {
            List<Command> robotCommands = robot.GetCommands();
            string username = (robot.isOpponent ? playerTurnObjectArray[1].name : playerTurnObjectArray[0].name);
            foreach (Command cmd in robotCommands)
            {
                cmd.robotId = robot.id;
                cmd.owner = username;
                commands.Add(cmd);
            }
            robot.ClearRobotCommands();
        }
        myturn = false;
        Logger.ClientLog("Sending Commands: " + commands.Count);
        GameClient.SendSubmitCommands(commands);
    }

    public static void PlayEvents(List<GameEvent> events)
    {
        uiController.StartCoroutine(EventsRoutine(events));
    }

    public static IEnumerator EventsRoutine(List<GameEvent> events)
    {
        Logger.ClientLog("Received Events: " + events.Count);
        foreach(GameEvent evt in events)
        {
            RobotController primaryRobot = Array.Find(robotControllers, (RobotController r) => r.id == evt.primaryRobotId);
            if (evt is GameEvent.Rotate)
            {
                GameEvent.Rotate rot = (GameEvent.Rotate)evt;
                primaryRobot.Rotate(Robot.OrientationToVector(rot.destinationDir));
            } else if (evt is GameEvent.Move)
            {
                GameEvent.Move mov = (GameEvent.Move)evt;
                primaryRobot.Place((int)mov.destinationPos.x, (int)mov.destinationPos.y);
            }
            else if (evt is GameEvent.Attack)
            {
                GameEvent.Attack atk = (GameEvent.Attack)evt;
                RobotController[] victims = Array.FindAll(robotControllers, (RobotController r) => Array.Exists(atk.victimIds, (short vid) => r.id == vid));
                for (int i = 0; i < victims.Length; i++)
                {
                    victims[i].SetHealth(atk.victimHealth[i]);
                }
            }
            else
            {
                Logger.ClientLog("ERROR: Unhandled Event - " + evt.ToString());
            }
            Logger.ClientLog("Output to history what happened: " + evt.ToString());
            yield return new WaitForSeconds(eventDelay);
        }
        Logger.ClientLog("Finished Events");
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
}

