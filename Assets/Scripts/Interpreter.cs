using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class Interpreter : MonoBehaviour {

    private static Game.Player[] playerTurnObjectArray;

    private static UIController uiController;
    private static BoardController boardController;
    private static RobotController[] robotControllers;
    public static int eventDelay = 1;
    public static string boardFile = GameConstants.PROTOBOARD_FILE;
    public static string[] myRobotNames = new string[0];
    public static string[] opponentRobotNames = new string[0];

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

    public static void LoadBoard(Game.Player[] ptos)
    {
        playerTurnObjectArray = ptos;
        SceneManager.LoadScene("Prototype");
    }

    public static void InitializeUI(UIController ui)
    {
        uiController = ui;
        uiController.InitializeUICanvas(playerTurnObjectArray);
    }

    public static void InitializeBoard(BoardController board)
    {
        boardController = board;
        boardController.InitializeBoard(boardFile);
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
                robotControllers[p1count + robotCount] = RobotController.Make(robot);
                robotControllers[p1count + robotCount].isOpponent = playerCount == 1;
                robotCount++;
            }
            p1count = robotCount;
            robotCount = 0;
            playerCount++;
        }
    }

    public static void SubmitActions()
    {
        List<Command> commands = new List<Command>();
        RobotController[] robots = FindObjectsOfType<RobotController>();
        foreach(RobotController robot in robots)
        {
            List<Command> robotCommands = robot.GetCommands();
            foreach(Command cmd in robotCommands)
            {
                cmd.robotId = robot.id;
                cmd.owner = (!GameConstants.LOCAL_MODE ? "ACTUAL_USERNAME":
                    (robot.IsOpponent() ? "opponent":"me"));
                commands.Add(cmd);
            }
            robot.ClearRobotCommands();
        }
        Logger.ClientLog("Sending Commands: " + commands.Count);
        GameClient.SendSubmitCommands(commands);
    }

    // TODO: Add time in between event
    public static void PlayEvents(List<GameEvent> events)
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
        }
        Logger.ClientLog("Finished Events");
    }
}

