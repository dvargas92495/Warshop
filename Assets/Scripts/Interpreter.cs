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
        int playerCount = 0;
        int robotCount = 0;
        int p1count = 0; //hack
        robotControllers = new RobotController[playerTurns[0].team.Length + playerTurns[1].team.Length];
        foreach (Game.Player player in playerTurns)
        {
            foreach(Robot robot in player.team)
            {
                RobotController r = RobotController.Load(robot);
                r.isOpponent = playerCount == 1;
                r.canCommand = !r.isOpponent;
                r.transform.GetChild(0).GetComponent<Image>().color = (r.isOpponent ? Color.red : Color.blue);
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
        Logger.ClientLog("Sending Commands: " + commands.Count);
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
        Logger.ClientLog("Received Events: " + events.Count);
        foreach(GameEvent evt in events)
        {
            RobotController primaryRobot = GetRobot(evt.primaryRobotId);
            if (evt is GameEvent.Rotate)
            {
                GameEvent.Rotate rot = (GameEvent.Rotate)evt;
                primaryRobot.displayRotate(Robot.OrientationToVector(rot.destinationDir));
            } else if (evt is GameEvent.Move)
            {
                GameEvent.Move mov = (GameEvent.Move)evt;
                primaryRobot.displayMove(mov.destinationPos);
            }
            else if (evt is GameEvent.Attack)
            {
                GameEvent.Attack atk = (GameEvent.Attack)evt;
                for (int i = 0; i < atk.victimIds.Length; i++)
                {
                    uiController.UpdateAttributes(atk.victimIds[i],atk.victimHealth[i], -1);
                }
            }
            else if (evt is GameEvent.Block)
            {
               //TODO: Blocked animation?
            }
            else if (evt is GameEvent.Push)
            {
                GameEvent.Push push = (GameEvent.Push)evt;
                primaryRobot.displayMove(push.transferPos);
                GetRobot(push.victim).displayMove(push.destinationPos);
            }
            else if (evt is GameEvent.Miss)
            {
                //TODO: Miss animation?
            }
            else if (evt is GameEvent.Battery)
            {
                //TODO: Battery animation?
            }
            else if (evt is GameEvent.Fail)
            {
                //TODO: Fail animation?
            }
            else if (evt is GameEvent.Death)
            {
                GameEvent.Death death = (GameEvent.Death)evt;
                primaryRobot.displayMove(death.returnLocation);
                uiController.UpdateAttributes(death.primaryRobotId, death.returnHealth, -1);
            }
            else if (evt is GameEvent.Poison)
            {
                //TODO: Poison animation?
            }
            else if (evt is GameEvent.Damage)
            {
                GameEvent.Damage dmg = (GameEvent.Damage)evt;
                uiController.UpdateAttributes(dmg.primaryRobotId, dmg.remainingHealth, -1);
            }
            else
            {
                Logger.ClientLog("ERROR: Unhandled Event - " + evt.ToString());
            }
            Logger.ClientLog("Output to history what happened: " + evt.ToString());
            uiController.SetBattery(evt.primaryBattery, evt.secondaryBattery);
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

    private static RobotController GetRobot(short id)
    {
        return Array.Find(robotControllers, (RobotController r) => r.id == id);
    }
}

