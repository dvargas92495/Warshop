using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SetupController : MonoBehaviour
{
    public Button backButton;
    public Button startGameButton;
    public RobotRosterPanelController robotRosterPanel;
    public SquadPanelController mySquadPanel;
    public SquadPanelController opponentSquadPanel;

    private byte myStarCount = 0;




    public InputField opponentName;
    public Text statusText;
    public TextAsset playtest;
    public Sprite[] robotDir;
    public GameObject maximizedRosterRobot;
    public GameObject robotSquadImage;
    public GameObject robotSelectedPanel;
    public GameObject squadPanelHolder;
    public GameObject maximizedRosterRobotInfoPanel;
    public string robotSelection;

    public Text starText;
    private bool loading;

    void Start ()
    {
        BaseGameManager.InitializeSetup(this);
        
        mySquadPanel.SetAddCallback(addSelectedToSquad);

        startGameButton.onClick.AddListener(StartGame);
        backButton.onClick.AddListener(EnterLobby);

        robotRosterPanel.SetMaximizeCallback(maximizeSelection);
        foreach (Sprite r in robotDir)
        {
            robotRosterPanel.AddRobotImage(r);
        }

        GameClient.ConnectToGameServer();
    }

    void Update()
    {
        startGameButton.interactable = (
            myStarCount == 8 &&
            mySquadPanel.squadPanelRobotHolder.transform.childCount <= 4
        );
        bool isError = !BaseGameManager.ErrorString.Equals("");
        statusText.transform.parent.gameObject.SetActive(isError || loading);
        statusText.color = isError ? Color.red : Color.white;
        statusText.text = isError ? BaseGameManager.ErrorString : statusText.text;
    }

    public void maximizeSelection(string selection)
    {
        if (selection != "no selection")
        {
            foreach (Sprite r in robotDir)
            {
                if (selection == r.name)
                {
                    robotSelectedPanel.SetActive(true);
                    maximizedRosterRobot.name = selection;
                    maximizedRosterRobot.GetComponent<Image>().sprite = r;
                    robotSelection = maximizedRosterRobot.name;
                    
                    TMP_Text[] fields = maximizedRosterRobotInfoPanel.GetComponentsInChildren<TMP_Text>();
                    fields[0].SetText(robotSelection);
                    Robot selected = Robot.create(robotSelection);
                    fields[1].SetText(selected.attack.ToString());
                    fields[2].SetText(selected.health.ToString());
                    fields[3].SetText(selected.description);

                    byte rating = (byte)selected.rating;
                    HorizontalLayoutGroup ratingGroup = maximizedRosterRobotInfoPanel.GetComponentInChildren<HorizontalLayoutGroup>();
                    for (int i = 0; i < ratingGroup.transform.childCount; i++)
                    {
                        ratingGroup.transform.GetChild(i).gameObject.SetActive(i < rating);
                    }

                    mySquadPanel.squadPanelButton.interactable = true;
                    if (GameConstants.LOCAL_MODE)
                    {
                        opponentSquadPanel.squadPanelButton.interactable = true;
                    }
                    

                }
            }
        }
        else
        {
            robotSelectedPanel.SetActive(false);
            robotSelection = "no selection";
            mySquadPanel.squadPanelButton.interactable = false;
            if (GameConstants.LOCAL_MODE)
            {
                opponentSquadPanel.squadPanelButton.interactable = false;
            }
        }


    }

    public void addSelectedToSquad(SquadPanelController squadPanel)
    {
        if (robotSelection != "no selection")
        {
            GameObject addedRobot = Instantiate(robotSquadImage, squadPanel.squadPanelRobotHolder.transform);
            addedRobot.name = robotSelection;
            addedRobot.GetComponent<Button>().onClick.AddListener(() => removeAddedFromSquad(squadPanel, addedRobot));
            foreach (Sprite r in robotDir)
            {
                if (robotSelection == r.name)
                {
                    addedRobot.GetComponent<Image>().sprite = r;
                }
            }
            if (squadPanel == mySquadPanel)
            {
                myStarCount += (byte)Robot.create(robotSelection).rating;
                starText.text = myStarCount.ToString() + "/8";
            }
            maximizeSelection("no selection");
        }
    }

    public void removeAddedFromSquad(SquadPanelController squadPanel, GameObject robotName)
    {
        if (squadPanel == mySquadPanel)
        {
            myStarCount -= (byte)Robot.create(robotName.name).rating;
            starText.text = myStarCount.ToString() + "/8";
        }
        Destroy(robotName);
    }

    public void ShowLoading()
    {
        loading = true;
        statusText.text = "Loading...";
    }

    void StartGame()
    {                           
        string[] myRosterStrings = new string[mySquadPanel.squadPanelRobotHolder.transform.childCount];
        for(int i = 0; i<myRosterStrings.Length; i++)
        {
            myRosterStrings[i] = mySquadPanel.squadPanelRobotHolder.transform.GetChild(i).name.Trim();
        }

        string[] opponentRosterStrings = new string[0];
        if (GameConstants.LOCAL_MODE)
        {
            opponentRosterStrings = new string[opponentSquadPanel.squadPanelRobotHolder.transform.childCount];
            for (int i = 0; i<opponentRosterStrings.Length; i++)
            {
                opponentRosterStrings[i] = opponentSquadPanel.squadPanelRobotHolder.transform.GetChild(i).name.Trim();
            }
        }


        StartGameImpl(
                (myRosterStrings),
                (GameConstants.LOCAL_MODE && opponentRosterStrings.Length > 0 ? opponentRosterStrings : new string[0]),
                GameClient.username,
                (opponentName.IsActive()? opponentName.text : "")
        );
    }

    void StartGameImpl(string[] mybots, string[] opbots, string myname, string opponentname)
    {
        string op = opponentname.Equals("") ? "opponent" : opponentname;
        op = op.Equals(myname) ? myname + "opponent" : op;
        ShowLoading();
        BaseGameManager.SendPlayerInfo(myname, op, mybots, opbots);
    }

    void EnterLobby()
    {
        SceneManager.LoadScene("Lobby");
    }
}
