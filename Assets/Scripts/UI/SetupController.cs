﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Linq;
using System.Collections.Specialized;

public class SetupController : MonoBehaviour {

    private byte myStarCount = 0;
    public Button loadBoardButton;
    public Button startGameButton;
    public InputField opponentName;
    public Text statusText;
    public TextAsset playtest;
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

    private Button mySquadsAddButton;
    private Button opponentSquadsAddButton;
    private Transform mySquadPanelRobotHolder;
    private Transform opponentSquadPanelRobotHolder;
    public Text starText;

    // Use this for initialization
    void Start ()
    {
        if (GameConstants.LOCAL_MODE && playtest != null)
        {
            string[] lines = playtest.text.Split('\n');
            StartGame(
                lines[1].Trim().Split(','),
                lines[2].Trim().Split(','),
                lines[3].Trim(),
                lines[4].Trim()
            );
            return;
        }
        
        Interpreter.setupController = this;
        RosterController.InitializeInitial(this);


        startGameButton.onClick.AddListener(() =>
            {                           
                string[] myRosterStrings = new string[mySquadPanelRobotHolder.transform.childCount];
                for(int i = 0; i < myRosterStrings.Length; i++)
                {
                    myRosterStrings[i] = mySquadPanelRobotHolder.transform.GetChild(i).name.Trim();
                }

                string[] opponentRosterStrings = new string[0];
                if (GameConstants.LOCAL_MODE)
                {
                    opponentRosterStrings = new string[opponentSquadPanelRobotHolder.transform.childCount];
                    for (int i = 0; i < opponentRosterStrings.Length; i++)
                    {
                        opponentRosterStrings[i] = opponentSquadPanelRobotHolder.transform.GetChild(i).name.Trim();
                    }
                }


                StartGame(
                      (myRosterStrings),
                      (GameConstants.LOCAL_MODE && opponentRosterStrings.Length > 0 ? opponentRosterStrings : new string[0]),
                      GameClient.username,
                      (opponentName.IsActive() ? opponentName.text : "")
                );
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
        GameClient.ConnectToGameServer();
    }

    void Update()
    {
        startGameButton.interactable = (
            myStarCount == 8 &&
            mySquadPanelRobotHolder.transform.childCount <= 4
        );
        if (!Interpreter.ErrorString.Equals(""))
        {
            statusText.transform.parent.gameObject.SetActive(true);
            statusText.color = Color.red;
            statusText.text = Interpreter.ErrorString;
        }
    }

    public void maximizeSelection(string selection)
    {
        GameObject robotSelectionPanel = robotSelectedPanel;
        foreach (Transform child in robotSelectionPanel.transform)
        {
            Destroy(child.gameObject);
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
                    Robot selected = Robot.create(robotSelection);
                    fields[1].SetText("Rating: " + selected.rating);
                    fields[2].SetText("Attack: " + selected.attack);
                    fields[3].SetText("Health: " + selected.health);
                    fields[4].SetText("Ability: " + selected.description);

                    mySquadsAddButton.interactable = true;
                    if (GameConstants.LOCAL_MODE)
                    {
                        opponentSquadsAddButton.interactable = true;
                    }
                    

                }
            }
        }
        else
        {
            robotSelection = "no selection";
            mySquadsAddButton.interactable = false;
            if (GameConstants.LOCAL_MODE)
            {
                opponentSquadsAddButton.interactable = false;
            }
        }


    }

    public void squadPanelsCreate(bool localMode)
    {
        GameObject mySquads = Instantiate(squadBackgroundPanel, squadPanelHolder.transform);
        mySquads.name = "My Squads";
        mySquads.GetComponent<Image>().color = Color.blue;
        createIndividualSquads(mySquads.transform, mySquads.name);
        if (localMode)
        { 
            GameObject opponentSquads = Instantiate(squadBackgroundPanel, squadPanelHolder.transform);
            opponentSquads.name = "Opponent Squads";
            opponentSquads.GetComponent<Image>().color = Color.red;
            opponentName.gameObject.SetActive(true);
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
            if (squadsOwner == "My Squads")
            {
                mySquadPanelRobotHolder = currentSquadPanel.transform.GetChild(1);
                mySquadsAddButton = currentSquadButton.GetComponent<Button>();
                mySquadsAddButton.onClick.AddListener(() => RosterController.addToSquad(squadsOwner, currentSquadPanel.name));
                mySquadsAddButton.GetComponent<Button>().interactable = false;
                mySquadsAddButton.GetComponent<Image>().sprite = squadSpriteDir[i];
            }
            if (squadsOwner == "Opponent Squads")
            {
                opponentSquadPanelRobotHolder = currentSquadPanel.transform.GetChild(1);
                opponentSquadsAddButton = currentSquadButton.GetComponent<Button>();
                opponentSquadsAddButton.onClick.AddListener(() => RosterController.addToSquad(squadsOwner, currentSquadPanel.name));
                opponentSquadsAddButton.GetComponent<Button>().interactable = false;
                opponentSquadsAddButton.GetComponent<Image>().sprite = squadSpriteDir[i];
            }
            
        }
    }

    public void addSelectedToSquad(string squadOwner, string squadName)
    {
        if (robotSelection != "no selection")
        {
            GameObject addedRobot;
            if (squadOwner == "My Squads")
            {
                addedRobot = Instantiate(robotSquadImage, mySquadPanelRobotHolder.transform);
            }
            else
            {
                 addedRobot = Instantiate(robotSquadImage, opponentSquadPanelRobotHolder.transform);
            }
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
                myStarCount += (byte)Robot.create(robotSelection).rating;
                starText.text = myStarCount.ToString() + "/8";
            }
            maximizeSelection("no selection");
        }
    }

    public void removeAddedFromSquad(string squadOwner, string squadName, GameObject robotName)
    {
        if (squadOwner == "My Squads")
        {
            myStarCount -= (byte)Robot.create(robotName.name).rating;
            starText.text = myStarCount.ToString() + "/8";
        }
        Destroy(robotName);
    }

    void StartGame(string[] mybots, string[] opbots, string myname, string opponentname)
    {
        string op = opponentname.Equals("") ? "opponent" : opponentname;
        op = op.Equals(myname) ? myname + "opponent" : op;
        statusText.transform.parent.gameObject.SetActive(true);
        statusText.color = Color.white;
        statusText.text = "Loading...";
        Interpreter.SendPlayerInfo(myname, op, mybots, opbots);
    }
}
