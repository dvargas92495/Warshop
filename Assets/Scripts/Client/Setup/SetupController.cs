using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SetupController : MonoBehaviour
{
    public Button backButton;
    public Button startGameButton;
    public MaximizedRosterRobotController maximizedRosterRobot;
    public RobotRosterPanelController robotRosterPanel;
    public Sprite[] robotDir;
    public SquadPanelController mySquadPanel;
    public SquadPanelController opponentSquadPanel;
    public Text statusText;
    public TextAsset playtest;

    private byte myStarCount = 0;




    public InputField opponentName;

    public Text starText;

    void Start ()
    {
        BaseGameManager.InitializeSetup(this);
        
        mySquadPanel.SetAddCallback(AddSelectedToMySquad);

        startGameButton.onClick.AddListener(StartGame);
        backButton.onClick.AddListener(EnterLobby);

        robotRosterPanel.SetMaximizeCallback(maximizeSelection);
        foreach (Sprite r in robotDir)
        {
            robotRosterPanel.AddRobotImage(r);
        }

        GameClient.ConnectToGameServer(DisplayError);
    }

    void EnterLobby()
    {
        SceneManager.LoadScene("Lobby");
    }

    public void maximizeSelection(Sprite selectedRobot)
    {
        maximizedRosterRobot.Select(selectedRobot);
        mySquadPanel.squadPanelButton.interactable = opponentSquadPanel.squadPanelButton.interactable = true;
    }

    public void AddSelectedToMySquad(SquadPanelController squadPanel)
    {
        myStarCount += maximizedRosterRobot.GetRating();
        UpdateStarText();
        AddSelectedToSquad(squadPanel);
    }

    public void AddSelectedToSquad(SquadPanelController squadPanel)
    {
        RobotSquadImageController addedRobot = squadPanel.AddRobotSquadImage();
        addedRobot.SetRemoveCallback(() => RemoveAddedFromSquad(squadPanel, addedRobot));
        addedRobot.SetSprite(maximizedRosterRobot.GetRobotSprite());
        addedRobot.SetRating(maximizedRosterRobot.GetRating());

        maximizedRosterRobot.Hide();
        mySquadPanel.squadPanelButton.interactable = opponentSquadPanel.squadPanelButton.interactable = false;
    }

    public void RemoveAddedFromSquad(SquadPanelController squadPanel, RobotSquadImageController robot)
    {
        if (squadPanel == mySquadPanel)
        {
            myStarCount -= robot.GetRating();
            UpdateStarText();
        }
        Destroy(robot);
    }

    public void ShowLoading()
    {
        statusText.transform.parent.gameObject.SetActive(true);
        statusText.color = Color.white;
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

    void UpdateStarText()
    {
        starText.text = myStarCount.ToString() + "/8";
        startGameButton.interactable = (
            myStarCount == 8 &&
            mySquadPanel.squadPanelRobotHolder.transform.childCount <= 4
        );
    }

    void DisplayError(string message)
    {
        statusText.transform.parent.gameObject.SetActive(true);
        statusText.color = Color.red;
        statusText.text = message;
    }
}
