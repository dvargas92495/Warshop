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
    public GameObject CommandSlot;
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
            AddCommandSlots(robotIdToUserPanel[userPlayer.team[i].id].transform, userPlayer.team[i].id, userPlayer.team[i].priority);
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
        return panel;
    }

    private void AddCommandSlots(Transform panel, short id, byte p)
    {
        Rect outer = panel.GetComponent<RectTransform>().rect;
        float startingY = 0.75f;
        float div = (1.0f / GameConstants.MAX_PRIORITY);
        float outerHeightPer = outer.height * startingY * div;
        float outerWidthPer = outer.width;
        float size = Mathf.Min(outerHeightPer, outerWidthPer);
        for (int i = GameConstants.MAX_PRIORITY; i > 0; i--)
        {
            RectTransform cmd = Instantiate(CommandSlot, panel).GetComponent<RectTransform>();
            cmd.anchorMax = new Vector2(size/outerWidthPer, startingY * (i - 1 + size/outerHeightPer) * div);
            cmd.anchorMin = new Vector2(1-size/outerWidthPer, startingY * (i - size/outerHeightPer) * div);
            if (i > p)
            {
                cmd.GetComponentInChildren<Image>().color = NO_COMMAND;
            }
            Button cmdDelete = cmd.GetComponentInChildren<Button>(true);
            cmdDelete.gameObject.SetActive(false);
            SetOnClickClear(cmdDelete, id, i);
        }
    }

    private void SetOnClickClear(Button b, short id, int i)
    {
        b.onClick.AddListener(() =>
        {
            ClearCommands(b.transform.parent.parent);
            Interpreter.DeleteCommand(id, i);
        });
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

    public void ClearCommands(Transform panel)
    {
        for (int i = 3; i < panel.childCount; i++)
        {
            Transform child = panel.GetChild(i);
            child.GetComponentInChildren<Button>(true).gameObject.SetActive(false);
            Image cmdPanel = child.GetComponentInChildren<Image>();
            cmdPanel.sprite = null;
            if (!cmdPanel.color.Equals(NO_COMMAND))
            {
                cmdPanel.color = OPEN_COMMAND;
                cmdPanel.rectTransform.rotation = Quaternion.Euler(Vector3.zero);
            }
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
            Image cmdPanel = panel.GetComponentInChildren<Image>();
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
            Image cmdPanel = panel.GetChild(i).GetComponentInChildren<Image>();
            if (cmdPanel.color.Equals(OPEN_COMMAND) || cmdPanel.color.Equals(HIGHLIGHTED_COMMAND))
            {
                cmdPanel.color = SUBMITTED_COMMAND;
                panel.GetChild(i).GetComponentInChildren<Button>(true).gameObject.SetActive(false);
            }
        }
    }

    public void addSubmittedCommand(Sprite cmd, byte d, short id)
    {
        Transform panel = robotIdToUserPanel[id].transform;
        for (int i = 3; i < panel.childCount; i++)
        {
            Transform child = panel.GetChild(i);
            Image cmdPanel = child.GetComponentInChildren<Image>();
            if (!cmdPanel.color.Equals(NO_COMMAND) && cmdPanel.sprite == null)
            {
                cmdPanel.sprite = cmd;
                Rect size = cmdPanel.rectTransform.rect;
                cmdPanel.rectTransform.Rotate(Vector3.forward * d * 90);
                if (d % 2 == 1) cmdPanel.rectTransform.rect.Set(size.x, size.y, size.height, size.width);
                child.GetComponentInChildren<Button>(true).gameObject.SetActive(child.GetComponentInChildren<Image>().color.Equals(OPEN_COMMAND));
                break;
            }
        }
    }

    public Tuple<string, byte>[] getCommandsSerialized(short id)
    {
        Transform panel = robotIdToUserPanel[id].transform;
        List<Tuple<string, byte>> content = new List<Tuple<string, byte>>();
        for (int i = 3; i < panel.childCount; i++)
        {
            Image cmdPanel = panel.GetChild(i).GetComponentInChildren<Image>();
            if (cmdPanel.color.Equals(NO_COMMAND)) continue;
            if (cmdPanel.sprite == null) break;
            content.Add(new Tuple<string, byte>(cmdPanel.sprite.name, (byte)(cmdPanel.rectTransform.localRotation.eulerAngles.z / 90)));
        }
        return content.ToArray();
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
