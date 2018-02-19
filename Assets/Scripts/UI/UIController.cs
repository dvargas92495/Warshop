using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;

public class UIController : MonoBehaviour {

	public Image BackgroundPanel;
    public TextMesh ScoreModel;

    public TMP_Text opponentNameText;
    internal TextMesh opponentScore;
    public GameObject OpponentsRobots;
    public GameObject opponentRobotPanel;

    public TMP_Text userNameText;
    internal TextMesh userScore;
    public GameObject UsersRobots;
    public GameObject userRobotPanel;
    public GameObject priorityArrow;

    public Button SubmitCommands;
    public Button StepBackButton;
    public Button StepForwardButton;
    public Button BackToPresent;
    public Text EventTitle;
    
    public Sprite[] sprites;
    public Camera boardCamera;

    private static Color NO_COMMAND = new Color(0.25f, 0.25f, 0.25f);
    private static Color HIGHLIGHTED_COMMAND = new Color(0.5f, 0.5f, 0.5f);
    private static Color SUBMITTED_COMMAND = new Color(0.75f, 0.75f, 0.75f);
    private static Color OPEN_COMMAND = new Color(1, 1, 1);
    private Dictionary<short, GameObject> robotIdToUserPanel = new Dictionary<short, GameObject>();
    private Dictionary<short, GameObject> robotIdToOpponentPanel = new Dictionary<short, GameObject>();

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
        if (GameConstants.LOCAL_MODE)
        {
            SetOpponentPlayerPanel(playerObjects[0]);
            SetUsersPlayerPanel(playerObjects[1]);
            robotIdToUserPanel.Values.ToList().ForEach((GameObject g) => g.SetActive(false));
            robotIdToOpponentPanel.Values.ToList().ForEach((GameObject g) => g.SetActive(false));
        }

        SetOpponentPlayerPanel(playerObjects[1]);
        SetUsersPlayerPanel(playerObjects[0]);

        SubmitCommands.onClick.AddListener(Interpreter.SubmitActions);
        BackToPresent.onClick.AddListener(Interpreter.BackToPresent);
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
            robotIdToOpponentPanel[opponentPlayer.team[i].id] = SetRobotPanel(opponentPlayer.team[i], opponentRobotPanel, OpponentsRobots.transform);
        }
    }

    void SetUsersPlayerPanel(Game.Player userPlayer)
    {
        userNameText.SetText(userPlayer.name + "'s Robots:");
        for (int i = 0; i < userPlayer.team.Length; i++)
        {
            robotIdToUserPanel[userPlayer.team[i].id] = SetRobotPanel(userPlayer.team[i], userRobotPanel, UsersRobots.transform);
        }
    }

    public GameObject SetRobotPanel(Robot r, GameObject reference, Transform parent)
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
                cmd.GetComponent<Image>().color = NO_COMMAND;
            } else if (minI == 0) minI = i;
            Button cmdDelete = cmd.GetComponentInChildren<Button>(true);
            cmdDelete.gameObject.SetActive(false);
            SetOnClickClear(cmdDelete, r.id, i, minI);
        }
        return panel;
    }

    public void SetPriority(int priority)
    {
        if (priority == -1)
        {
            priorityArrow.SetActive(false);
            return;
        }
        else if (priority == 0)
        {
            return;
        }
        int pos = 3 + 8 - priority;
        Transform lastRobotPanal = UsersRobots.transform.GetChild(UsersRobots.transform.childCount - 1);
        RectTransform anchor = lastRobotPanal.GetChild(pos).GetComponent<RectTransform>();
        RectTransform arrowRect = priorityArrow.GetComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(anchor.rect.width, anchor.rect.height);
        arrowRect.position = anchor.position;
        Vector2 translation = new Vector2(anchor.rect.width + 10, 0);
        arrowRect.anchoredPosition += translation;
        priorityArrow.SetActive(true);
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
            child.GetComponentInChildren<Button>(true).gameObject.SetActive(false);
            Image cmdPanel = child.GetComponent<Image>();
            cmdPanel.sprite = null;
            if (cmdPanel.color.Equals(HIGHLIGHTED_COMMAND) || cmdPanel.color.Equals(SUBMITTED_COMMAND)) cmdPanel.color = OPEN_COMMAND;
        }
    }

    public void ClearCommands(short id)
    {
        ClearCommands(robotIdToUserPanel[id].transform);
    }

    public void HighlightCommands(Type t, byte p)
    {
        foreach (short id in robotIdToUserPanel.Keys)
        {
            Transform robotPanel = robotIdToUserPanel[id].transform;
            if (robotPanel.childCount - p < 0) continue;
            Transform panel = robotPanel.GetChild(robotPanel.childCount - p);
            Image cmdPanel = panel.GetComponent<Image>();
            if (cmdPanel.sprite != null && cmdPanel.sprite.name.StartsWith(t.ToString().Substring("Command.".Length)))
            {
                cmdPanel.color = HIGHLIGHTED_COMMAND; ;
            }
        }
    }

    public void ColorCommandsSubmitted(short id)
    {
        Transform panel = robotIdToUserPanel[id].transform;
        for (int i = 3; i < panel.childCount; i++)
        {
            Image cmdPanel = panel.GetChild(i).GetComponent<Image>();
            if (cmdPanel.color.Equals(OPEN_COMMAND) || cmdPanel.color.Equals(HIGHLIGHTED_COMMAND))
            {
                cmdPanel.color = SUBMITTED_COMMAND;
                panel.GetChild(i).GetComponentInChildren<Button>(true).gameObject.SetActive(false);
            }
        }
    }

    public void addSubmittedCommand(Sprite cmd, short id)
    {
        Transform panel = robotIdToUserPanel[id].transform;
        for (int i = 3; i < panel.childCount; i++)
        {
            Transform child = panel.GetChild(i);
            Image cmdPanel = child.GetComponent<Image>();
            if (!cmdPanel.color.Equals(NO_COMMAND) && cmdPanel.sprite == null)
            {
                cmdPanel.sprite = cmd;
                child.GetComponentInChildren<Button>(true).gameObject.SetActive(child.GetComponent<Image>().color.Equals(OPEN_COMMAND));
                break;
            }
        }
    }

    public string[] getCommandText(short id)
    {
        Transform panel = robotIdToUserPanel[id].transform;
        string[] texts = new string[GameConstants.MAX_PRIORITY];
        for (int i = 3; i < panel.childCount; i++)
        {
            Sprite s = panel.GetChild(i).GetComponent<Image>().sprite;
            texts[i-3] = (s == null ? "" : s.name);
        }
        return texts;
    }

    public void setCommandText(Sprite[] texts, short id)
    {
        Transform panel = robotIdToUserPanel[id].transform;
        for (int i = 3; i < panel.childCount; i++)
        {
            panel.GetChild(i).GetComponent<Image>().sprite = texts[i - 3];
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
        if (!isPrimary) Interpreter.Flip();
    }

    public void Flip()
    {
        boardCamera.transform.Rotate(new Vector3(0, 0, 180));
        Interpreter.boardController.allQueueLocations.ToList().ForEach((TileController t) =>
        {
            t.GetComponent<SpriteRenderer>().flipY = !t.GetComponent<SpriteRenderer>().flipY;
            t.GetComponent<SpriteRenderer>().flipX = !t.GetComponent<SpriteRenderer>().flipX;
        });
        userScore.transform.Rotate(Vector3.forward, 180);
        opponentScore.transform.Rotate(Vector3.forward, 180);
        if (GameConstants.LOCAL_MODE)
        {
            SetButtons(true);
            LightUpPanel(false, true);
            string u = userNameText.text;
            userNameText.text = opponentNameText.text;
            opponentNameText.text = u;
            robotIdToUserPanel.Values.ToList().ForEach((GameObject g) => g.SetActive(!g.activeInHierarchy));
            robotIdToOpponentPanel.Values.ToList().ForEach((GameObject g) => g.SetActive(!g.activeInHierarchy));
        }
    }

    public void SetButtons(bool b)
    {
        StepBackButton.interactable = StepForwardButton.interactable = BackToPresent.interactable = SubmitCommands.interactable = b;
    }

    public void LightUpPanel(bool bright, bool isUser)
    {
        Image panel = (isUser ? UsersRobots : OpponentsRobots).transform.parent.GetComponent<Image>();
        Color regular = (isUser ? new Color(0, 0.5f, 1.0f, 1.0f) : new Color(1.0f, 0, 0, 1.0f));
        float mult = (bright ? 1.0f : 0.5f);
        panel.color = new Color(regular.r * mult, regular.g*mult, regular.b * mult, regular.a * mult);
    }

}
