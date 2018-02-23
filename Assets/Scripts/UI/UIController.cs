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

    public TMP_Text userNameText;
    internal TextMesh userScore;
    public GameObject UsersRobots;
    public GameObject RobotPanel;
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
        opponentNameText.SetText(opponentPlayer.name);
        for (int i = 0; i < opponentPlayer.team.Length; i++)
        {
            SetRobotPanel(opponentPlayer.team[i], OpponentsRobots.transform);
        }
    }

    void SetUsersPlayerPanel(Game.Player userPlayer)
    {
        userNameText.SetText(userPlayer.name);
        for (int i = 0; i < userPlayer.team.Length; i++)
        {
            robotIdToPanel[userPlayer.team[i].id] = SetRobotPanel(userPlayer.team[i], UsersRobots.transform);
        }
    }

    public GameObject SetRobotPanel(Robot r, Transform parent)
    {
        GameObject panel = Instantiate(RobotPanel, parent);
        panel.name = "Robot" + r.id;
        Transform icon = panel.transform.GetChild(1);
        icon.GetComponentInChildren<Image>().sprite = Array.Find(sprites, (Sprite s) => s.name.Equals(r.name));
        TMP_Text[] fields = panel.GetComponentsInChildren<TMP_Text>();
        fields[0].SetText(r.name);
        fields[1].SetText(r.description);
        AddCommandSlots(panel.transform.GetChild(3), r.id, r.priority);
        robotIdToPanel[r.id] = panel;
        return panel;
    }

    private void AddCommandSlots(Transform panel, short id, byte p)
    {
        Rect outer = panel.GetComponent<RectTransform>().rect;
        for (int i = GameConstants.MAX_PRIORITY; i > 0; i--)
        {
            RectTransform cmd = Instantiate(CommandSlot, panel).GetComponent<RectTransform>();
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
        int pos = GameConstants.MAX_PRIORITY - priority;
        Transform lastRobotPanal = UsersRobots.transform.GetChild(UsersRobots.transform.childCount - 1);
        RectTransform anchor = lastRobotPanal.GetChild(3).GetChild(pos).GetComponent<RectTransform>();
        RectTransform arrowRect = priorityArrow.GetComponent<RectTransform>();
        arrowRect.sizeDelta = new Vector2(anchor.rect.width, anchor.rect.height);
        arrowRect.position = anchor.position;
        Vector2 translation = new Vector2(anchor.rect.width + 10, 0);
        arrowRect.anchoredPosition += translation;
        priorityArrow.SetActive(true);
    }

    public void ClearCommands(Transform panel)
    {
        for (int i = 0; i < panel.childCount; i++)
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
        ClearCommands(robotIdToPanel[id].transform.GetChild(3));
    }

    public void HighlightCommands(Type t, byte p)
    {
        foreach (short id in robotIdToPanel.Keys)
        {
            Transform commandPanel = robotIdToPanel[id].transform.GetChild(3);
            if (commandPanel.childCount - p < 0) continue;
            Transform panel = commandPanel.GetChild(commandPanel.childCount - p);
            Image cmdPanel = panel.GetComponentInChildren<Image>();
            if (cmdPanel.sprite != null && cmdPanel.sprite.name.StartsWith(t.ToString().Substring("Command.".Length)))
            {
                cmdPanel.color = HIGHLIGHTED_COMMAND; ;
            }
        }
    }

    public void ColorCommandsSubmitted(short id)
    {
        Transform panel = robotIdToPanel[id].transform.GetChild(3);
        for (int i = 0; i < panel.childCount; i++)
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
        Transform panel = robotIdToPanel[id].transform.GetChild(3);
        for (int i = 0; i < panel.childCount; i++)
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
        Transform panel = robotIdToPanel[id].transform.GetChild(3);
        List<Tuple<string, byte>> content = new List<Tuple<string, byte>>();
        for (int i = 0; i < panel.childCount; i++)
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
        float xMin = UsersRobots.transform.parent.GetComponent<RectTransform>().anchorMax.x;
        float xMax = OpponentsRobots.transform.parent.GetComponent<RectTransform>().anchorMin.x;
        boardCamera.rect = new Rect(xMin, 0, xMax - xMin, 1);
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
