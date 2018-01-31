﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIController : MonoBehaviour {

	public Image BackgroundPanel;
    private GameObject OpponentsRobots;
    private GameObject UsersRobots;
    private GameObject PlayerTurnTextObject;
	private GameObject PlayerAPanel;
	private GameObject PlayerBPanel;
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
    public GameObject opponentRobotPanel;
    public GameObject userRobotPanel;
    public Sprite[] sprites;
    public Camera boardCamera;

	private GameObject robotImagePanel;

	private string[] playerNames;
	private Sprite robotSprite;
    private bool myTurn;

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
        OpponentsRobots = GameObject.Find("OpponentRobots");
        SetOpponentPlayerPanel(playerObjects[1], OpponentsRobots.transform);

        // Set User Player Panel & Robots
        UsersRobots = GameObject.Find("UserRobots");
        SetUsersPlayerPanel(playerObjects[0], UsersRobots.transform);





        //TMP_Text  playerTurnText = getChildTMP_Text(BackgroundPanel, "PlayerTurnText");

        //// playerturntextobject = getchildgameobject (backgroundpanel, "playerturntext");

        //PlayerAPanel = getChildGameObject(BackgroundPanel, "PlayerAPanel");
        //PlayerBPanel = getChildGameObject(BackgroundPanel, "PlayerBPanel");
        //GameObject[] playerPanels = { PlayerAPanel, PlayerBPanel };

        //// set the components of the uicanvas
        //SetPlayerTurnText(playerTurnText, playerTurnObjects[0]);
        //SetPlayerPanels(playerPanels, playerTurnObjects);

    }

    void SetOpponentPlayerPanel(Game.Player opponentPlayer, Transform parentObject)
    {
        TMP_Text opponentNameText = getChildTMP_Text(BackgroundPanel.gameObject, "OpponentNameText");
        opponentNameText.SetText(opponentPlayer.name + "'s Robots:");

        for (int i = 0; i < opponentPlayer.team.Length; i++)
        {
           GameObject opponentRobot = Instantiate(opponentRobotPanel, parentObject);
           opponentRobot.name = "Opponent" + opponentPlayer.team[i].name;

        }

        
    }

    void SetUsersPlayerPanel(Game.Player userPlayer, Transform parentObject)
    {
        TMP_Text userNameText = getChildTMP_Text(BackgroundPanel.gameObject, "UserNameText");
        userNameText.SetText(userPlayer.name + "'s Robots:");

        for (int i = 0; i < userPlayer.team.Length; i++)
        {
            GameObject userRobot = Instantiate(userRobotPanel, parentObject);
            userRobot.name = "User" + userPlayer.team[i].name;

        }


    }

    // Set's header text of UICanvas
    void SetPlayerTurnText(TMP_Text playerTurnText, Game.Player currentPlayer)
	{
		playerTurnText.SetText(currentPlayer.name + "'s Turn");
	}

	// Sets each players panels on the UICanvas (Contains robot info)
	void SetPlayerPanels (GameObject[] PlayerPanels, Game.Player[] PlayerTurnObjects)
	{
        // for each playerPanel
        // Set headertext
        // for each robot
        // get correct panel
        //attach info
        SetBattery(PlayerTurnObjects[0].battery, PlayerTurnObjects[1].battery);
		for (int i = 0; i < PlayerPanels.Length; i++) {

			TMP_Text playerPanelHeader = getChildTMP_Text(PlayerPanels [i], "Player Robots Summary");

			playerPanelHeader.SetText(PlayerTurnObjects[i].name);

			for (int j = 1; j < 1 + PlayerTurnObjects[i].team.Length; j++){
				string currentRobotInfoPanel = "RobotInfoPanel (" + (j) + ")";
				Robot currentRobot = PlayerTurnObjects[i].team[j-1];
				robotInfoPanel = getChildGameObject(PlayerPanels[i], currentRobotInfoPanel);
//			
//
//				//Robot name 
				TMP_Text robotInfoPanelRobotText = getChildTMP_Text(robotInfoPanel, "RobotName");
				robotInfoPanelRobotText.SetText(currentRobot.name);
//
				//Robot sprite
				robotInfoPanelRobotSprite = getChildGameObject(robotInfoPanel, "RobotSprite");
				attachRobotSprite(robotInfoPanelRobotSprite, currentRobot.name);
//
//				//Robot attributes

				TMP_Text robotInfoPanelRobotAttributes = getChildTMP_Text (robotInfoPanel, "Attributes");
				robotInfoPanelRobotAttributes.SetText("A: " + currentRobot.attack.ToString() + " P: " + currentRobot.priority.ToString() + " H: " + currentRobot.health.ToString());

			}
		}

	}

	void attachRobotSprite(GameObject robotImagePanel, string robotName){
        robotSprite = Array.Find(sprites, (Sprite s) => s.name.StartsWith(robotName));
		robotImagePanel.GetComponent<Image>().sprite = robotSprite;
	}

	GameObject getChildGameObject(GameObject parentGameObject, string searchName) {
		Transform[] childGameObjects = parentGameObject.transform.GetComponentsInChildren<Transform>(true);
		foreach (Transform transform in childGameObjects) {
			if (transform.gameObject.name == searchName) {
				return transform.gameObject;
			} 
		}
		return null;
	}

	TMP_Text getChildTMP_Text(GameObject parentGameObject, string searchName) {
		TMP_Text[] childGameObjects = parentGameObject.GetComponentsInChildren<TMP_Text>();
		foreach (TMP_Text TMPtext in childGameObjects) {
			if (TMPtext.name == searchName) {
				return TMPtext;
			} 
		}
		return null;
	}

    // Modal functions
    public void ShowHandButtonPress()
    {
        
       modalPanelObject.SetActive(true);
       cancelButton.onClick.RemoveAllListeners();
       cancelButton.onClick.AddListener(ClosePanel);

       cancelButton.gameObject.SetActive(true);
    }

    public void SubmitActionsButtonPress()
    {
        Interpreter.SubmitActions();
        submittedActions.Clear();
    }

    public void ClearActionsButtonPress()
    {
        submittedActions.Clear();
    }



    public void ShowQueuedActionsButtonPress()
    {
        formatActionsModalTextLines(submittedActions);
        modalPanelObject.SetActive(true);
        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(ClosePanel);
        cancelButton.gameObject.SetActive(true);
    }

    public void addSubmittedCommand(Command cmd, string robotIdentifier)
    {
        string CommandText = robotIdentifier + " " + cmd.ToString();
        submittedActions.Add(CommandText);
        //Dropdown ActionsDropdown = GameObject.Find("Submitted Actions Dropdown").GetComponent<Dropdown>();
        //ActionsDropdown.AddOptions(submittedActions);
    }

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
    }

    public void Flip()
    {
        boardCamera.transform.Rotate(new Vector3(0, 0, 180));
        BackgroundPanel.transform.Rotate(new Vector3(0, 0, 180));
    }

    public void UpdateAttributes(RobotController currentRobot)
    {
        TMP_Text robotInfoPanelRobotAttributes = getChildTMP_Text(robotInfoPanel, "Attributes");
        robotInfoPanelRobotAttributes.SetText("A: " + currentRobot.attack.ToString() + " P: " + currentRobot.priority.ToString() + " H: " + currentRobot.health.ToString());
    }

    public void SetBattery(int a, int b)
    {
        TMP_Text aHeader = getChildTMP_Text(PlayerAPanel, "Score");
        TMP_Text bHeader = getChildTMP_Text(PlayerBPanel, "Score");
        aHeader.text = a.ToString();
        bHeader.text = b.ToString();
    }

    public void PositionCamera(bool isPrimary)
    {
        boardCamera.transform.position = new Vector3(Interpreter.boardController.boardCellsWide, Interpreter.boardController.boardCellsHeight);
        //float z = -boardCamera.transform.position.z;
        //RectTransform rect = BackgroundPanel.GetComponent<RectTransform>();
        //boardCamera.fieldOfView = Mathf.Atan2(Interpreter.boardController.boardCellsHeight * 0.5f, z) * Mathf.Rad2Deg * 2;
        //Vector3 bottomLeftPoint = boardCamera.ViewportToWorldPoint(new Vector3(rect.anchorMax.x, 0, z));
        //Interpreter.boardController.transform.position = bottomLeftPoint + new Vector3(0.5f, 0.5f);
    }
}
