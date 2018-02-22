using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Networking;
using TMPro;
using System.Linq;
using System.Collections.Specialized;

public class InitialController : MonoBehaviour {

    private bool isServer;
    private byte myStarCount = 0;
    public Button loadBoardButton;
    public Button startGameButton;
    public InputField myName;
    public InputField opponentName;
    public Button myAdd;
    public Button opponentAdd;
    public Dropdown mySelect;
    public Dropdown opponentSelect;
    public Text myRoster;
    public Text opponentRoster;
    public Text statusText;
    public TextAsset keys;
    public Toggle localModeToggle;
    public Toggle useServerToggle;
    public TextAsset playtest;
    public TextAsset[] boardfiles;
    public RobotController robotBase;
    public Sprite[] robotDir;
    public Sprite[] squadSpriteDir;
    public GameObject robotRosterImage;
    public GameObject maximizedRosterRobot;
    public GameObject robotSquadImage;
    public GameObject robotRosterPanel;
    public GameObject robotSelectedPanel;
    public GameObject squadPanelHolder;
    public GameObject squadBackgroundPanel;
    public GameObject squadPanel;
    public GameObject maximizedRosterRobotInfoPanel;
    public string robotSelection;


    //TEMP JUST FOR PLAYTEST: DELETE
    public Text starText;
    private Dictionary<string, byte[]> robotDictionary = new Dictionary<string, byte[]>()
    {
        // Rating, attack, health
        {"Bronze Grunt", new byte[] {1,2,3}},
        {"Silver Grunt", new byte[] {2,3,8}},
        {"Golden Grunt", new byte[] {3,5,10}},
        {"Platinum Grunt",new byte[] {4,6,15}},
    };
    private Dictionary<string, string[]> abilityDictionary = new Dictionary<string, string[]>()
    {
        // Rating, attack, health
        {"Bronze Grunt", new string[] {"None",""}},
        {"Silver Grunt", new string[] {"None",""}},
        {"Golden Grunt", new string[] {"None",""}},
        {"Platinum Grunt",new string[] {"None",""}},
    };

    public void Awake()
    {
        isServer = (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null);
    }

    // Use this for initialization
    void Start ()
    {
        RosterController.InitializeInitial(this);
        Logger.Setup(isServer);
        App.LinkAssets(boardfiles);
        if (isServer)
        {
            GameConstants.USE_SERVER = true;
            App.StartServer();
            return;
        }
        if (keys != null)
        {
            string[] lines = keys.text.Split('\n');
            GameConstants.AWS_PUBLIC_KEY = lines[0].Trim();
            GameConstants.AWS_SECRET_KEY = lines[1].Trim();
        } else
        {
            GameConstants.LOCAL_MODE = true;
            GameConstants.USE_SERVER = false;
            //localModeToggle.gameObject.SetActive(false);
            //useServerToggle.gameObject.SetActive(false);
            useServerToggle.interactable = false;
        }
        if (Application.isEditor && playtest != null)
        {
            string[] lines = playtest.text.Split('\n');
            StartGame(
                lines[0].Trim(),
                lines[1].Trim().Split(','),
                lines[2].Trim().Split(','),
                lines[3].Trim(),
                lines[4].Trim()
            );
            return;
        }
        UnityAction<bool> opponentToggle = (bool val) =>
        {
            GameConstants.LOCAL_MODE = val;
            opponentName.gameObject.SetActive(val);
            opponentAdd.gameObject.SetActive(val);
            opponentSelect.gameObject.SetActive(val);
            opponentRoster.gameObject.SetActive(val);
        };
        UnityAction<bool> awsToggle = (bool val) =>
        {
            GameConstants.USE_SERVER = val;
        };
        if (Application.isEditor)
        {
            localModeToggle.onValueChanged.AddListener(opponentToggle);
            useServerToggle.onValueChanged.AddListener(awsToggle);
        } else
        {
            opponentToggle(false);
            awsToggle(true);
            localModeToggle.gameObject.SetActive(false);
            useServerToggle.gameObject.SetActive(false);
        }


        startGameButton.onClick.AddListener(() =>
            {                           
                if (myStarCount == 8)
                {
                   GameObject squadPanelRobotHolder = GameObject.Find("SquadPanelsHolder/My Squads/Squad Panel0/SquadPanelRobotHolder");
                   string[] myRosterStrings = new string[squadPanelRobotHolder.transform.childCount];
                   for(int i = 0; i < myRosterStrings.Length; i++)
                   {
                        myRosterStrings[i] = squadPanelRobotHolder.transform.GetChild(i).name.Trim();
                   }

                    GameObject oppSquadPanelRobotHolder = GameObject.Find("SquadPanelsHolder/Opponent Squads/Squad Panel0/SquadPanelRobotHolder");
                    string[] opponentRosterStrings = new string[oppSquadPanelRobotHolder.transform.childCount];
                    for (int i = 0; i < opponentRosterStrings.Length; i++)
                    {
                        opponentRosterStrings[i] = oppSquadPanelRobotHolder.transform.GetChild(i).name.Trim();
                    }


                    StartGame(
                          "Battery",
                          (myRosterStrings),
                          (GameConstants.LOCAL_MODE && opponentRosterStrings.Length > 0 ? opponentRosterStrings : new string[0]),
                          myName.text,
                          (opponentName.IsActive() ? opponentName.text : "")
                      );
                }
            }
        );

        

       
        foreach (Sprite r in robotDir)
        {
            GameObject rosterRobot = Instantiate(robotRosterImage, robotRosterPanel.transform);
            rosterRobot.name = r.name;
            rosterRobot.GetComponent<Image>().sprite = r;
            rosterRobot.GetComponent<Button>().onClick.AddListener(() => RosterController.maximizeRobot(rosterRobot.name));


        }


        squadPanelsCreate(GameConstants.LOCAL_MODE);

    }

    void Update()
    {
       // if (Input.GetKeyUp(KeyCode.Space)) { myRoster.text = ""; starText.text = "0/8 STARS"; }
    }


   public void maximizeSelection(string selection)
    {
        GameObject robotSelectionPanel = robotSelectedPanel;
        foreach (Transform child in robotSelectionPanel.transform)
        {
            GameObject.Destroy(child.gameObject);
        }
        if (selection != "no selection")
        {
           
            foreach (Sprite r in robotDir)
            {
                if (selection == r.name)
                {
                    GameObject selectedRobot = Instantiate(maximizedRosterRobot, robotSelectedPanel.transform);
                    selectedRobot.name = selection;
                    selectedRobot.GetComponent<Image>().sprite = r;
                    robotSelection = selectedRobot.name;

                    GameObject selectedRobotInfo = Instantiate(maximizedRosterRobotInfoPanel, robotSelectedPanel.transform);
                    TMP_Text[] fields = selectedRobotInfo.GetComponentsInChildren<TMP_Text>();
                    fields[0].SetText(robotSelection);
                    if (robotDictionary[robotSelection][0] == 1)
                    {
                        fields[1].SetText("Rating: Bronze");
                    }
                    if (robotDictionary[robotSelection][0] == 2)
                    {
                        fields[1].SetText("Rating: Silver");
                    }
                    if (robotDictionary[robotSelection][0] == 3)
                    {
                        fields[1].SetText("Rating: Gold");
                    }
                    if (robotDictionary[robotSelection][0] == 4)
                    {
                        fields[1].SetText("Rating: Platinum");
                    }
                    fields[2].SetText("Attack: " + robotDictionary[robotSelection][1].ToString());
                    fields[3].SetText("Health: " + robotDictionary[robotSelection][2].ToString());
                    fields[4].SetText("Ability: " + abilityDictionary[robotSelection][0] + abilityDictionary[robotSelection][1]);


                }
            }
        }
        else
        {
            robotSelection = "no selection";
        }


    }

    public void squadPanelsCreate(bool localMode)
    {
        GameObject squadPanelsHolder = squadPanelHolder;
        GameObject mySquads = Instantiate(squadBackgroundPanel, squadPanelsHolder.transform);
        mySquads.name = "My Squads";
        createIndividualSquads(mySquads.transform, mySquads.name);
        if (localMode)
        { 
            GameObject opponentSquads = Instantiate(squadBackgroundPanel, squadPanelsHolder.transform);
            opponentSquads.name = "Opponent Squads";
            createIndividualSquads(opponentSquads.transform, opponentSquads.name);
        }

    }

    public void createIndividualSquads(Transform squadBackground, string squadsOwner)
    {
        for (int i = 0; i < 1; i++)
        {
            GameObject currentSquadPanel = Instantiate(squadPanel, squadBackground);
            currentSquadPanel.name = "Squad Panel" + i.ToString();
            Transform currentSquadButton = currentSquadPanel.transform.GetChild(0);
            currentSquadButton.GetComponent<Button>().onClick.AddListener(() => RosterController.addToSquad(squadsOwner, currentSquadPanel.name));
            currentSquadButton.GetComponent<Image>().sprite = squadSpriteDir[i];
        }
    }

    public void addSelectedToSquad(string squadOwner, string squadName)
    {
        if (robotSelection != "no selection")
        {
            GameObject squadPanelRobotHolder = GameObject.Find(squadOwner + "/" + squadName + "/SquadPanelRobotHolder");
            GameObject addedRobot = Instantiate(robotSquadImage, squadPanelRobotHolder.transform);
            addedRobot.name = robotSelection;
            addedRobot.GetComponent<Button>().onClick.AddListener(() => RosterController.removeFromSquad(squadOwner, squadName, addedRobot));
            foreach (Sprite r in robotDir)
            {
                if (robotSelection == r.name)
                {
                    addedRobot.GetComponent<Image>().sprite = r;
                }
            }
            if (squadOwner == "My Squads")
            {
                myStarCount += robotDictionary[robotSelection][0];
                starText.text = myStarCount.ToString() + "/8";
                if (myStarCount == 8)
                {
                    startGameButton.interactable = true;
                }
                else
                {
                    startGameButton.interactable = false;
                }
            }
 
            maximizeSelection("no selection");
        }
    }

    public void removeAddedFromSquad(string squadOwner, string squadName, GameObject robotName)
    {
        if (squadOwner == "My Squads")
        {
            myStarCount -= robotDictionary[robotName.name][0];
            starText.text = myStarCount.ToString() + "/8";
            if (myStarCount == 8)
            {
                startGameButton.interactable = true;
            }
            else
            {
                startGameButton.interactable = false;
            }
        }
        GameObject.Destroy(robotName);
    }

    void StartGame(string b, string[] mybots, string[] opbots, string myname, string opponentname)
    {

        RobotController.robotBase = robotBase;
        RobotController.robotDir = robotDir;
        Interpreter.myRobotNames = mybots;
        Interpreter.opponentRobotNames = opbots;
        Interpreter.initialController = this;
        statusText.color = Color.black;
        statusText.text = "Loading...";
        Interpreter.ConnectToServer(myname, opponentname, b);
    }
}
