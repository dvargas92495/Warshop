using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIController : MonoBehaviour {

	public Image BackgroundPanel;

    public TMP_Text opponentNameText;
    public TMP_Text opponentScore;
    public GameObject OpponentsRobots;
    public GameObject opponentRobotPanel;

    public TMP_Text userNameText;
    public TMP_Text userScore;
    public GameObject UsersRobots;
    public GameObject userRobotPanel;

    public Button SubmitCommands;



    private GameObject robotInfoPanel;
	private GameObject robotInfoPanelRobotName;
	private GameObject robotInfoPanelRobotSprite;
	private GameObject robotInfoPanelRobotAttributes;
	private GameObject robotInfoPanelRobotStatus;
	private GameObject robotInfoPanelRobotEquipment;
    private GameObject modalTextBackdrop;
    private GameObject modalDisplayPanel;

    public List<string> submittedActions = new List<string>();

    public Text placeholder;
    public GameObject modalPanelObject;
    public Button cancelButton;
    public Sprite[] sprites;
    public Camera boardCamera;

	private GameObject robotImagePanel;

	private Sprite robotSprite;

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
    public void InitializeUICanvas(Game.Player[] playerObjects)
    {
        // Set Opponent Player Panel & Robots
        SetOpponentPlayerPanel(playerObjects[1]);

        // Set User Player Panel & Robots
        SetUsersPlayerPanel(playerObjects[0]);

        SubmitCommands.onClick.AddListener(() =>
        {
            Interpreter.SubmitActions();
            submittedActions.Clear();
        });

        SetBattery(playerObjects[0].battery, playerObjects[1].battery);
    }

    void SetOpponentPlayerPanel(Game.Player opponentPlayer)
    {
        opponentNameText.SetText(opponentPlayer.name + "'s Robots:");
        for (int i = 0; i < opponentPlayer.team.Length; i++)
        {
            string name = opponentPlayer.team[i].name;
            GameObject opponentRobot = Instantiate(opponentRobotPanel, OpponentsRobots.transform);
            opponentRobot.name = "Opponent" + name;
            opponentRobot.transform.GetChild(1).GetComponent<Image>().sprite = Array.Find(sprites, (Sprite s) => s.name.Equals(name));
            TMP_Text[] fields = opponentRobot.GetComponentsInChildren<TMP_Text>();
            fields[0].SetText(name);
            fields[1].SetText(opponentPlayer.team[i].description);
        }
    }

    void SetUsersPlayerPanel(Game.Player userPlayer)
    {
        userNameText.SetText(userPlayer.name + "'s Robots:");
        for (int i = 0; i < userPlayer.team.Length; i++)
        {
            string name = userPlayer.team[i].name;
            GameObject userRobot = Instantiate(userRobotPanel.gameObject, UsersRobots.transform);
            userRobot.name = "User" + userPlayer.team[i].name;
            userRobot.name = "Opponent" + name;
            userRobot.transform.GetChild(1).GetComponent<Image>().sprite = Array.Find(sprites, (Sprite s) => s.name.Equals(name));
            TMP_Text[] fields = userRobot.GetComponentsInChildren<TMP_Text>();
            fields[0].SetText(name);
            fields[1].SetText(userPlayer.team[i].description);
        }
    }

    public void addSubmittedCommand(Command cmd, string robotIdentifier)
    {
        string CommandText = robotIdentifier + " " + cmd.ToString();
        submittedActions.Add(CommandText);
        //Dropdown ActionsDropdown = GameObject.Find("Submitted Actions Dropdown").GetComponent<Dropdown>();
        //ActionsDropdown.AddOptions(submittedActions);
    }

    /*
    public void resetModal()
    {
        modalDisplayPanel = getChildGameObject(modalPanelObject, "ModalDisplay");
        modalTextBackdrop = getChildGameObject(modalDisplayPanel, "Text Backdrop");
        foreach (Transform child in modalTextBackdrop.transform)
        {
            Destroy(child.gameObject);
        }
    }
    public void formatActionsModalTextLines(List<string> textLines)
    {
        // grab size of whole box, 390, 575
        modalDisplayPanel = getChildGameObject(modalPanelObject, "ModalDisplay");
        modalTextBackdrop = getChildGameObject(modalDisplayPanel, "Text Backdrop");
        float xWidth = 390f - 35;
        //float yWidth = 575f;
        float ySpacer = 0;
        float yStart = -200;
        // For each textline in textLines, create new gameObject with text in it
        for (int i = 0; i < textLines.Count; i++)
        {
            GameObject textBox = new GameObject("textBox");
            textBox.transform.SetParent(modalTextBackdrop.transform);
            Text textToAdd = textBox.AddComponent<Text>();
            textToAdd.text = textLines[i];
            textToAdd.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            textToAdd.transform.Rotate(new Vector3(180, 0, 0));
            textToAdd.transform.localScale = new Vector3(1.15f, 2.5f, 0);
            textToAdd.transform.localPosition = new Vector3(35f, ySpacer + yStart, 0f);
            textToAdd.rectTransform.sizeDelta= new Vector2(xWidth,50);
            ySpacer = ySpacer + 75;

        }
    }

    void ClosePanel()
    {
        modalPanelObject.SetActive(false);
        resetModal();
    }*/

    public void Flip()
    {
        boardCamera.transform.Rotate(new Vector3(0, 0, 180));
    }

    public void UpdateAttributes(RobotController currentRobot)
    {
        //TMP_Text robotInfoPanelRobotAttributes = getChildTMP_Text(robotInfoPanel, "Attributes");
        //robotInfoPanelRobotAttributes.SetText("A: " + currentRobot.attack.ToString() + " P: " + currentRobot.priority.ToString() + " H: " + currentRobot.health.ToString());
    }

    public void SetBattery(int a, int b)
    {
        userScore.SetText(a.ToString());
        opponentScore.SetText(b.ToString());
    }

    public void PositionCamera(bool isPrimary)
    {
        float x = BackgroundPanel.GetComponent<RectTransform>().anchorMax.x;
        boardCamera.rect = new Rect(x, 0, 1 - x, 1);
        boardCamera.transform.localPosition = new Vector3(Interpreter.boardController.boardCellsWide-1, Interpreter.boardController.boardCellsHeight-1,-2)/2;
        boardCamera.orthographicSize = Interpreter.boardController.boardCellsHeight / 2;
        if (!isPrimary) Flip();
    }
}
