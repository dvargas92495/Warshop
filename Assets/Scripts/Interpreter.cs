using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using Z8.Generic;

public class Interpreter : MonoBehaviour {

    private static PlayerTurnObject[] playerTurnObjectArray;

    private static UIController uiController;
    private static BoardController boardController;
    public static int eventDelay = 1;
    public static string boardFile = GameConstants.PROTOBOARD_FILE;
    public static string[] myRobots = new string[0];
    public static string[] opponentRobots = new string[0];

    void FixedUpdate()
    {
        if (Application.isEditor)
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                SceneManager.LoadScene("Initial");
            }
        }
    }

    public static void ConnectToServer()
    {
        GameClient.Initialize();
    }

    public static void SendPlayerInfo()
    {
        if (GameConstants.LOCAL_MODE)
        {
            GameClient.SendLocalGameRequest(myRobots, opponentRobots);
        }
        else
        {
            GameClient.SendGameRequest(myRobots);
        }
    }

    public static void LoadBoard(PlayerTurnObject[] ptos)
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

    private static void InitializeRobots(PlayerTurnObject[] playerTurns)
    {
        int playerCount = 0;
        int robotCount = 0;
        foreach (PlayerTurnObject player in playerTurns)
        {
            foreach(RobotObject robot in player.robotObjects)
            {
                robot.Owner = player.PlayerName;
                robot.Identifier = robot.Owner + " " + robot.Name;
                RobotController.Make(robot);
                boardController.PlaceRobotInQueue(robot.Identifier, playerCount == 1, robotCount);
                robotCount++;
            }
            robotCount = 0;
            playerCount++;
        }
    }

    public static void SubmitActions()
    {
        Debug.Log("Interpreter received end turn button");
        List<RobotCommand> commands = new List<RobotCommand>();
        RobotController[] robots = FindObjectsOfType<RobotController>();
        foreach(RobotController robot in robots)
        {
            List<RobotCommand> robotCommands = robot.GetCommands();
            foreach(RobotCommand cmd in robotCommands)
            {
                cmd.id = robot.GetId();
                cmd.owner = (!GameConstants.LOCAL_MODE ? "ACTUAL_USERNAME":
                    (robot.IsOpponent() ? "opponent":"me"));
                cmd.isOpponent = robot.IsOpponent();
                commands.Add(cmd);
            }
            robot.ClearRobotCommands();
        }
        Debug.Log("Interpreter received commands");
        GameClient.SendSubmitCommands(commands);
    }

    // TODO: Add time in between event
    public static void PlayEvents(List<GameEvent> events)
    {
        Debug.Log("Received Events");
        foreach(GameEvent evt in events)
        {
            evt.playEvent();
            Debug.Log("Output to history what happened: " + evt.ToString());
        }
        Debug.Log("Finished Events");
    }

    //TODO: Move this to Server
    private List<GameEvent> DummyServer(List<RobotCommand> commands)
    {
        commands.Sort((a, b) =>
        {
            RobotController aRobot = FindObjectsOfType<RobotController>().First((c) => c.GetId() == a.id && c.IsOpponent() == a.isOpponent);
            RobotController bRobot = FindObjectsOfType<RobotController>().First((c) => c.GetId() == b.id && c.IsOpponent() == b.isOpponent);
            return -1;
        });
        List<GameEvent> events = new List<GameEvent>();
        foreach(RobotCommand cmd in commands)
        {
            GameEvent e = null;
            if (cmd is SpawnCommand)
            {
                SpawnEvent evt = new SpawnEvent();
                evt.spawnIndex = ((SpawnCommand)cmd).getSpawnIndex();
                e = evt;
            } else if (cmd is MoveCommand)
            {
                MoveEvent evt = new MoveEvent();
                evt.direction = ((MoveCommand)cmd).getDirection();
                e = evt;
            }
            else if (cmd is RotateCommand)
            {
                RotateEvent evt = new RotateEvent();
                evt.direction = ((RotateCommand)cmd).getDirection();
                e = evt;

            }
            else if (cmd is AttackCommand)
            {
                AttackEvent evt = new AttackEvent();
                //evt.direction = ((AttackCommand)cmd).getDirection();
                e = evt;

            }
            else
            {
                Debug.Log("Bad Command: " + cmd.toString());
                continue;
            }
            e.primaryRobot = FindObjectsOfType<RobotController>().First((c) => c.GetId() == cmd.id && c.IsOpponent() == cmd.isOpponent);
            e.board = boardController;
            e.isOpponent = cmd.isOpponent;
            events.Add(e);
        }
        return events;
    }
}

