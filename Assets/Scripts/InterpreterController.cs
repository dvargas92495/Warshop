using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using Z8.Generic;

public class InterpreterController : MonoBehaviour {

    //Variables

    private PlayerTurnObject[] playerTurnObjectArray;

    private UIController UIController;
    private BoardController BoardController;
    public static int eventDelay = 1;
    public static string boardFile = GameConstants.PROTOBOARD_FILE;
    public static string[] playerARobots = new string[0];
    public static string[] playerBRobots = new string[0];
    private ClientController ClientController;
    private List<TurnObject> completedTurns = new List<TurnObject>();

    // TODO: Define game start object
    private class GameStartObject
    {

    }

    void FixedUpdate()
    {
        if (GameConstants.LOCAL_MODE)
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                SceneManager.LoadScene("Initial");
            }
        }
    }

    // TODO: Define turn object
    private class TurnObject
    {

    }

    // Initialize game by reading in turn info from config files.  Later will be server call
    void Awake () {
        //string mattsObj = parseConfigs();
        // Call model.iniialize(mattsObj), return turnResolveObj

        //dummyParseConfigs will  
        playerTurnObjectArray = DummyParseConfigs();

        ClientController = new ClientController();
        ClientInializationObject clientInitObject = ClientController.Initialize();
        GameStartObject gameStartObject = TranslateClientInit(clientInitObject);

        UIController = this.gameObject.GetComponent<UIController>();
        UIController.InitializeUICanvas(playerTurnObjectArray);
        BoardController = FindObjectOfType<BoardController>();

        // TODO: Change InitializeProtoBoard to take board layout as an argument
        // i.e.
        //
        // BoardLayout boardLayout = gameStartObject.GetBoardLayout();
        // ProtoBoardController.InitializeProtoBoard(boardLayout);
        BoardController.InitializeBoard(boardFile);

        // TODO: Add initialization code
        //
        // RobotObject[] playerRobots = gameStartObject.GetPlayerRobots();
        // RobotObject[] opponentRobots = gameStartObject.GetOpponentRobots();

        // function to get robots from playerTurnObjectArray
        // returns robots with info to send to ->

        // TODO: Change InitializeRobots code to initialize player/opponent robots seperately
        InitializeRobots(playerTurnObjectArray);



        //foreach (PlayerTurnObject player in playerTurnObjectArray)
        //{
        //  foreach (RobotObject robot in player.robotObjects)
        // {   //Option 1, factory method that requires RobotController to do all the setting and know what RobotObject looks like
        //    RobotController.Make(robot);
        //Option 2, would be to have interpretercontroller call a bunch of setter methods
        //RobotController rc = Instantiate(Resources.Load(blah blah blah...
        //rc.setId(robot.Id);
        //rc.setAttack(robot.Attack);
        //etc

        //I prefer option 2, but I'm currently too lazy to write all the setter methods
        //This double for loop should probably be in its own method too.
        //}
        //}
    }

	void Start () 
	{
	}

	// Parse the configs file to initialize game
	private PlayerTurnObject[] DummyParseConfigs(){

		// load text files
		TextAsset playerAContent = Resources.Load<TextAsset>("PlaytestFiles/PlayerA");
		TextAsset playerBContent = Resources.Load<TextAsset>("PlaytestFiles/PlayerB");

		// Break into lines
		string[] playerAlines = playerAContent.text.Split('\n');
		string[] playerBlines = playerBContent.text.Split('\n');

		// Function to return playerTurnObject from playerXLines

		PlayerTurnObject playerATurnObject = LineParser(playerAlines);
		PlayerTurnObject playerBTurnObject = LineParser(playerBlines);

		PlayerTurnObject[] playerTurnObjectArray = { playerATurnObject, playerBTurnObject };

		return playerTurnObjectArray;
	}

    // TODO: Define translator for game initialization
    private GameStartObject TranslateClientInit(ClientInializationObject clientInitObject)
    {
        return new GameStartObject();
    }

    // TODO: Define message generator for turns
    private string MessageGenerator()
    {
        return "hi";
    }

    // TODO: Define translator for turn responses
    private TurnObject TranslateTurnResponse(string turnResponse)
    {
        return new TurnObject();
    }

	// Parse lines of config files for initialize game (dummyParseConfigs)

	public PlayerTurnObject LineParser(string[] playerXLines){
		int[] lengthIds = playerXLines[0].Trim().Split(null).Select(int.Parse).ToArray();

		//Player name
		PlayerTurnObject playerXTurnObject = new PlayerTurnObject(playerXLines[1].Trim());


		//Robot ids
		int currentLine = 2;
		for (int i = 0; i < lengthIds [0]; i++) {
            string line = playerXLines[currentLine + i].Trim();
            string[] stats = line.Split(' '); //Order of stats follow order in RobotObject
            RobotObject currentRobot = new RobotObject() {
                Id = int.Parse(stats[0]), //Mimics spreadsheet row on Playtest Skeleton
                Name = stats[1],
                Health = int.Parse(stats[2]),
                Attack = int.Parse(stats[3]),
                Priority = int.Parse(stats[4]),
                Status = stats[5],
                Equipment = stats[6],
                IsOpponent = playerXTurnObject.PlayerName == "PlayerBName",
            };
            playerXTurnObject.AddRobot(currentRobot);
		}

		currentLine += lengthIds[0];

		//Card ids
		for (int i = 0; i < 4; i++) {
			playerXTurnObject.AddCard (int.Parse(playerXLines[currentLine + 1].Trim()));
		}
		return playerXTurnObject;
	}

    private void InitializeRobots(PlayerTurnObject[] playerTurns)
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
                BoardController.PlaceRobotInQueue(robot.Identifier, playerCount == 1, robotCount);
                robotCount++;
            }
            robotCount = 0;
            playerCount++;
        }
    }

    public void SubmitActions()
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
        }
        Debug.Log("Interpreter received commands");
        // TODO: Replace to sending request to actual server
        //
        // string commandMessage = MessageGenerator(commands);
        // completedTurns.Add(TranslateTurnResponse(ClientController.SubmitTurn(commandMessage)));
        // PlayEvents(completedTurns.Last().GetEvents());
        ClientController.SubmitTurn("Hello World");
        DummyServer(commands);
        //List<GameEvent> events = DummyServer(commands);
        //StartCoroutine(PlayEvents(events));
    }

    // TODO: Add play events code
    public IEnumerator PlayEvents(List<GameEvent> events)
    {
        foreach(GameEvent evt in events)
        {
            evt.playEvent();
            Debug.Log("Output to history what happened: " + evt.ToString());
            yield return new WaitForSeconds(eventDelay);
        }
    }

    private string ParseConfigs()
    {
        return "Matt's object";
    }

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

            }
            else if (cmd is AttackCommand)
            {

            }
            else
            {
                Debug.Log("Bad Command: " + cmd.toString());
                continue;
            }
            e.primaryRobot = FindObjectsOfType<RobotController>().First((c) => c.GetId() == cmd.id && c.IsOpponent() == cmd.isOpponent);
            e.board = BoardController;
            e.isOpponent = cmd.isOpponent;
            events.Add(e);
        }
        return events;
    }
}

