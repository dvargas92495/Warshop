using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using Z8.Generic;

public class UIController : MonoBehaviour {

	private GameObject BackgroundPanel;
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


	private GameObject robotImagePanel;

	private string[] playerNames;
	private Sprite robotSprite;
    private bool myTurn;

    void Start()
    {
        Interpreter.InitializeUI(this);
    }

    //Loads the UICanvas and it's child components
    public void InitializeUICanvas(PlayerTurnObject[] playerTurnObjects) 
	{
        // get child components  
        BackgroundPanel = GameObject.Find("UICanvas");

        TMP_Text  playerTurnText = getChildTMP_Text(BackgroundPanel, "PlayerTurnText");

        // playerturntextobject = getchildgameobject (backgroundpanel, "playerturntext");

        PlayerAPanel = getChildGameObject(BackgroundPanel, "PlayerAPanel");
        PlayerBPanel = getChildGameObject(BackgroundPanel, "PlayerBPanel");
        GameObject[] playerPanels = { PlayerAPanel, PlayerBPanel };

        // set the components of the uicanvas
        SetPlayerTurnText(playerTurnText, playerTurnObjects[0]);
        SetPlayerPanels(playerPanels, playerTurnObjects);
        myTurn = true;

	}

	// Set's header text of UICanvas
	void SetPlayerTurnText(TMP_Text playerTurnText, PlayerTurnObject currentPlayer)
	{
		playerTurnText.SetText(currentPlayer.PlayerName + "'s Turn");
	}

	// Sets each players panels on the UICanvas (Contains robot info)
	void SetPlayerPanels (GameObject[] PlayerPanels, PlayerTurnObject[] PlayerTurnObjects)
	{
		// for each playerPanel
			// Set headertext
			// for each robot
				// get correct panel
				//attach info

		for (int i = 0; i < PlayerPanels.Length; i++) {

			TMP_Text playerPanelHeader = getChildTMP_Text(PlayerPanels [i], "Player Robots Summary");

			playerPanelHeader.SetText(PlayerTurnObjects[i].PlayerName);

			for (int j = 1; j < 1 + PlayerTurnObjects[i].robotObjects.Count; j++){
				string currentRobotInfoPanel = "RobotInfoPanel (" + (j) + ")";
				RobotObject currentRobot = PlayerTurnObjects[i].robotObjects[j-1];
				robotInfoPanel = getChildGameObject(PlayerPanels[i], currentRobotInfoPanel);
//			
//
//				//Robot name 
				TMP_Text robotInfoPanelRobotText = getChildTMP_Text(robotInfoPanel, "RobotName");
				robotInfoPanelRobotText.SetText(currentRobot.Name);
//
				//Robot sprite
				robotInfoPanelRobotSprite = getChildGameObject(robotInfoPanel, "RobotSprite");
				attachRobotSprite(robotInfoPanelRobotSprite, currentRobot.Name);
//
//				//Robot attributes

				TMP_Text robotInfoPanelRobotAttributes = getChildTMP_Text (robotInfoPanel, "Attributes");
				robotInfoPanelRobotAttributes.SetText("A: " + currentRobot.Attack.ToString() + " P: " + currentRobot.Priority.ToString() + " H: " + currentRobot.Health.ToString());
//
//				//Robot status

				TMP_Text robotInfoPanelRobotStatus = getChildTMP_Text (robotInfoPanel, "Status");
				robotInfoPanelRobotStatus.SetText("Status: " + currentRobot.Status);

//				//Robot equipment
				TMP_Text robotInfoPanelRobotEquipment = getChildTMP_Text (robotInfoPanel, "Equipment");
				robotInfoPanelRobotEquipment.SetText("Eq: " + currentRobot.Equipment);
			}
		}

	}

	void attachRobotSprite(GameObject robotImagePanel, string robotName){
		string spriteDir = "Robots/Sprites/" + robotName + " small";
		robotSprite = Resources.Load<Sprite>(spriteDir);
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
        GameObject Controllers = GameObject.Find("Controllers");
        if (!GameConstants.LOCAL_MODE || !myTurn) //Send if not Local, or is Local and opponent just submitted
        {
            Interpreter.SubmitActions();
        } else if (GameConstants.LOCAL_MODE)
        {
            myTurn = !myTurn;
        }
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
            GameObject.Destroy(child.gameObject);
        }
    }
    public void formatActionsModalTextLines(List<string> textLines)
    {
        // grab size of whole box, 390, 575
        modalDisplayPanel = getChildGameObject(modalPanelObject, "ModalDisplay");
        modalTextBackdrop = getChildGameObject(modalDisplayPanel, "Text Backdrop");
        float xWidth = 390f - 35;
        float yWidth = 575f;
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


}
