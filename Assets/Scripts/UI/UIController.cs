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

    public void SetRobotPanel(Robot r, GameObject reference, Transform parent)
    {
        GameObject panel = Instantiate(reference, parent);
        panel.name = "Robot" + r.id;
        Transform icon = panel.transform.GetChild(1);
        icon.GetComponent<Image>().sprite = Array.Find(sprites, (Sprite s) => s.name.Equals(r.name));
        icon.GetChild(0).GetComponentInChildren<Text>().text = r.health.ToString();
        icon.GetChild(1).GetComponentInChildren<Text>().text = r.attack.ToString();
        TMP_Text[] fields = panel.GetComponentsInChildren<TMP_Text>();
        fields[0].SetText(name);
        fields[1].SetText(r.description);
    }

    public void addSubmittedCommand(Command cmd, string robotIdentifier)
    {
        string CommandText = robotIdentifier + " " + cmd.ToString();
        submittedActions.Add(CommandText);
        //Dropdown ActionsDropdown = GameObject.Find("Submitted Actions Dropdown").GetComponent<Dropdown>();
        //ActionsDropdown.AddOptions(submittedActions);
    }

    public void UpdateAttributes(short id, short health, short attack)
    {
        for (int i = 0; i < UsersRobots.transform.childCount; i++)
        {
            if (UsersRobots.transform.GetChild(i).name.Equals("Robot" + id))
            {
                UpdateAttributes(UsersRobots.transform.GetChild(i), health, attack);
                return;
            }
        }
        for (int i = 0; i < OpponentsRobots.transform.childCount; i++)
        {
            if (OpponentsRobots.transform.GetChild(i).name.Equals("Robot" + id))
            {
                UpdateAttributes(OpponentsRobots.transform.GetChild(i), health, attack);
                return;
            }
        }
    }

    private void UpdateAttributes(Transform panel, short health, short attack)
    {
        panel.transform.GetChild(1).GetChild(0).GetComponentInChildren<Text>().text = health.ToString();
        if (attack >= 0) panel.transform.GetChild(1).GetChild(1).GetComponentInChildren<Text>().text = attack.ToString();
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

    public void Flip()
    {
        boardCamera.transform.Rotate(new Vector3(0, 0, 180));
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
}
