using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Z8.Generic;

public class InterpreterController : MonoBehaviour {

	//Variables

	private static PlayerTurnObject[] playerTurnObjectArray;

    private UIController UIController;
    private ProtoBoardController ProtoBoardController;

	// Initialize game by reading in turn info from config files.  Later will be server call
	void Awake () {
		//string mattsObj = parseConfigs();
		// Call model.iniialize(mattsObj), return turnResolveObj

		//dummyParseConfigs will 
		playerTurnObjectArray = DummyParseConfigs();
        UIController = this.gameObject.GetComponent<UIController>();
        UIController.InitializeUICanvas(playerTurnObjectArray);
        ProtoBoardController = FindObjectOfType<ProtoBoardController>();
        ProtoBoardController.InitializeProtoBoard();

        // function to get robots from playerTurnObjectArray
        // returns robots with info to send to ->
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
                ProtoBoardController.PlaceRobotInQueue(robot.Identifier, playerCount == 1, robotCount);
                robotCount++;
            }
            robotCount = 0;
            playerCount++;
        }
    }

    public void SubmitActions()
    {
        Debug.Log("Interpreter received actions");
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
                commands.Add(cmd);
            }
        }
        DummyServer(commands); //TODO: Replace to sending request to actual server
    }

    public void PlayEvents()
    {

    }

	// Dummy data 
	public static string[] GetPlayerNames(){
		string playerAName = playerTurnObjectArray[0].PlayerName;
		string playerBName = playerTurnObjectArray[1].PlayerName;
		string[] playerNames = {playerAName, playerBName};
		return playerNames;
    }

    private string ParseConfigs()
    {
        return "Matt's object";
    }

    private void DummyServer(List<RobotCommand> commands)
    {
        commands.Sort((a, b) =>
        {
            RobotController aRobot = FindObjectsOfType<RobotController>().First((c) => c.GetId() == a.id);
            RobotController bRobot = FindObjectsOfType<RobotController>().First((c) => c.GetId() == b.id);
            return -2;
        });
    }
}

