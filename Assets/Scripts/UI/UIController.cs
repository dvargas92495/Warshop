using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIController : MonoBehaviour {

	public Image BackgroundPanel;
    public TextMesh ScoreModel;

    public TMP_Text opponentNameText;
    public TextMesh opponentScore;
    public GameObject OpponentsRobots;
    public GameObject opponentRobotPanel;

    public TMP_Text userNameText;
    public TextMesh userScore;
    public GameObject UsersRobots;
    public GameObject userRobotPanel;

    public Image EventModal;
    public Button SubmitCommands;
    public Button EventButton;
    public Button StepBackButton;
    public Button StepForwardButton;
    public Text EventTitle;
    public Button CancelButton;
    public Text EventLog;
    
    public Sprite[] sprites;
    public Camera boardCamera;

    private Dictionary<short, GameObject> robotIdToPanel = new Dictionary<short, GameObject>();

    void Start()
    {
        Interpreter.InitializeUI(this);
    }

    void Update()
    {
        if (Application.isEditor)
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                SceneManager.LoadScene("Initial");
            }
        }
    }

    //Loads the UICanvas and it's child components
    public void InitializeUICanvas(Game.Player[] playerObjects, bool isPrimary)
    {
        // Set Opponent Player Panel & Robots
        SetOpponentPlayerPanel(playerObjects[1]);

        // Set User Player Panel & Robots
        SetUsersPlayerPanel(playerObjects[0]);

        SubmitCommands.onClick.AddListener(() =>
        {
            Interpreter.SubmitActions();
            foreach(short id in robotIdToPanel.Keys)
            {
                ClearCommands(robotIdToPanel[id].transform);
            }
        });

        EventButton.onClick.AddListener(() => EventModal.gameObject.SetActive(!EventModal.gameObject.activeInHierarchy));
        CancelButton.onClick.AddListener(() => EventModal.gameObject.SetActive(false));
        StepBackButton.onClick.AddListener(Interpreter.StepBackward);
        StepForwardButton.onClick.AddListener(Interpreter.StepForward);

        userScore = Instantiate(ScoreModel, Interpreter.boardController.GetVoidTile(isPrimary).transform);
        userScore.GetComponent<MeshRenderer>().sortingOrder = 1;
        opponentScore = Instantiate(ScoreModel, Interpreter.boardController.GetVoidTile(!isPrimary).transform);
        opponentScore.GetComponent<MeshRenderer>().sortingOrder = 1;
        SetBattery(playerObjects[0].battery, playerObjects[1].battery);
        PositionCamera(isPrimary);
    }

    void SetOpponentPlayerPanel(Game.Player opponentPlayer)
    {
        opponentNameText.SetText(opponentPlayer.name + "'s Robots:");
        for (int i = 0; i < opponentPlayer.team.Length; i++)
        {
            SetRobotPanel(opponentPlayer.team[i], opponentRobotPanel, OpponentsRobots.transform);
        }
    }

    void SetUsersPlayerPanel(Game.Player userPlayer)
    {
        userNameText.SetText(userPlayer.name + "'s Robots:");
        for (int i = 0; i < userPlayer.team.Length; i++)
        {
            SetRobotPanel(userPlayer.team[i], userRobotPanel, UsersRobots.transform);
        }
    }

    Game.Player GetFromPanelAndDestroy(bool isUser)
    {
        string nametext = (isUser ? userNameText.text : opponentNameText.text);
        string name = nametext.Substring(0, nametext.IndexOf("'s Robots:"));
        GameObject panelContainer = (isUser ? UsersRobots : OpponentsRobots);
        Robot[] team = new Robot[panelContainer.transform.childCount];
        for (int i = 0; i<panelContainer.transform.childCount; i++)
        {
            Transform child = panelContainer.transform.GetChild(i);
            TMP_Text[] fields = child.GetComponentsInChildren<TMP_Text>();
            Robot r = Robot.create(fields[0].text);
            r.id = short.Parse(child.name.Substring("Robot".Length));
            team[i] = r;
            Destroy(child.gameObject);
        }
        Game.Player p = new Game.Player(team, name);
        p.battery = short.Parse(isUser ? userScore.text : opponentScore.text);
        return p;
    }

    public void SetRobotPanel(Robot r, GameObject reference, Transform parent)
    {
        GameObject panel = Instantiate(reference, parent);
        panel.name = "Robot" + r.id;
        Transform icon = panel.transform.GetChild(1);
        icon.GetComponent<Image>().sprite = Array.Find(sprites, (Sprite s) => s.name.Equals(r.name));
        TMP_Text[] fields = panel.GetComponentsInChildren<TMP_Text>();
        fields[0].SetText(r.name);
        fields[1].SetText(r.description);
        int minI = 0;
        for (int i = 3; i < panel.transform.childCount; i++)
        {
            Transform cmd = panel.transform.GetChild(i);
            if (panel.transform.childCount - i > r.priority)
            {
                cmd.GetComponent<Image>().color = Color.gray;
            } else if (minI == 0) minI = i;
            Button cmdDelete = cmd.GetComponentInChildren<Button>(true);
            cmdDelete.gameObject.SetActive(false);
            SetOnClickClear(cmdDelete, r.id, i, minI);
        }
        robotIdToPanel[r.id] = panel;
    }

    private void SetOnClickClear(Button b, short id, int i, int mI)
    {
        b.onClick.AddListener(() =>
        {
            ClearCommands(b.transform.parent.parent);
            Interpreter.DeleteCommand(id, i - mI);
        });
    }

    public void ClearCommands(Transform panel)
    {
        for (int i = 3; i < panel.childCount; i++)
        {
            Transform child = panel.GetChild(i);
            child.GetComponentInChildren<Text>().text = "";
            child.GetComponentInChildren<Button>(true).gameObject.SetActive(false);
        }
    }

    public void addSubmittedCommand(Command cmd, short id)
    {
        Transform panel = robotIdToPanel[id].transform;
        for (int i = 3; i < panel.childCount; i++)
        {
            Transform child = panel.GetChild(i);
            if (!child.GetComponent<Image>().color.Equals(Color.gray) && child.GetComponentInChildren<Text>().text.Equals(""))
            {
                child.GetComponentInChildren<Text>().text = cmd.ToString();
                child.GetComponentInChildren<Button>(true).gameObject.SetActive(true);
                break;
            }
        }
    }

    public void SetBattery(int a, int b)
    {
        userScore.text = a.ToString();
        opponentScore.text = b.ToString();
    }

    public int GetUserBattery()
    {
        return int.Parse(userScore.text);
    }

    public int GetOpponentBattery()
    {
        return int.Parse(opponentScore.text);
    }

    public void PositionCamera(bool isPrimary)
    {
        float x = BackgroundPanel.GetComponent<RectTransform>().anchorMax.x;
        boardCamera.rect = new Rect(x, 0, 1 - x, 1);
        boardCamera.transform.localPosition = new Vector3(Interpreter.boardController.boardCellsWide-1, Interpreter.boardController.boardCellsHeight-1,-20)/2;
        boardCamera.orthographicSize = Interpreter.boardController.boardCellsHeight / 2;
        if (!isPrimary) Flip();
    }

    public void Flip()
    {
        boardCamera.transform.Rotate(new Vector3(0, 0, 180));
        if (GameConstants.LOCAL_MODE)
        {
            Game.Player user = GetFromPanelAndDestroy(true);
            Game.Player opponent = GetFromPanelAndDestroy(false);
            SetUsersPlayerPanel(opponent);
            SetOpponentPlayerPanel(user);
        }
    }

    public void DisplayEvent(string s)
    {
        EventLog.text += s + ".\n";
    }

    public void StartEventModal(int turn, byte p)
    {
        EventTitle.text = "TURN " + turn + " - PRIORITY " + p;
        EventLog.text = "";
        EventModal.gameObject.SetActive(true);
    }
}
