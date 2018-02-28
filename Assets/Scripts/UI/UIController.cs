using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;

public class UIController : MonoBehaviour {

	public TextMesh ScoreModel;

    public TMP_Text opponentNameText;
    internal TextMesh opponentScore;
    public GameObject OpponentsRobots;

    public TMP_Text userNameText;
    internal TextMesh userScore;
    public GameObject UsersRobots;
    public GameObject RobotPanel;
    public CommandSlotController CommandSlot;
    public GameObject priorityArrow;

    public Button OpponentSubmit;
    public Button SubmitCommands;
    public Button StepBackButton;
    public Button StepForwardButton;
    public Button BackToPresent;
    public Image SplashScreen;
    
    public Sprite[] sprites;
    public Sprite[] arrows;
    public Camera boardCamera;

    private Dictionary<short, GameObject> robotIdToPanel = new Dictionary<short, GameObject>();

    void Start()
    {
        Interpreter.InitializeUI(this);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.B))
        {
            BackToInitial();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
#endif
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        for (KeyCode i = KeyCode.Alpha1;i < KeyCode.Alpha9; i++)
        {
            if (Input.GetKeyDown(i)) Interpreter.SelectRobot(i - KeyCode.Alpha0);
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Interpreter.ClickMenuItem(Command.Spawn.DISPLAY);
        } else if (Input.GetKeyDown(KeyCode.M))
        {
            Interpreter.ClickMenuItem(Command.Move.DISPLAY);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            Interpreter.ClickMenuItem(Command.Attack.DISPLAY);
        }
        if (Input.GetKeyDown(KeyCode.UpArrow)){
            Interpreter.ClickSubmenuItem(Command.UP);
        } else if (Input.GetKeyDown(KeyCode.DownArrow)){
            Interpreter.ClickSubmenuItem(Command.DOWN);
        } else if(Input.GetKeyDown(KeyCode.LeftArrow)){
            Interpreter.ClickSubmenuItem(Command.LEFT);
        } else if (Input.GetKeyDown(KeyCode.RightArrow)){
            Interpreter.ClickSubmenuItem(Command.RIGHT);
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Interpreter.SubmitActions();
        }
    }

    public void BackToInitial()
    {
        SceneManager.LoadScene("Initial");
    }

    //Loads the UICanvas and it's child components
    public void InitializeUICanvas(Game.Player[] playerObjects, bool isPrimary)
    {
        SetOpponentPlayerPanel(playerObjects[1]);
        SetUsersPlayerPanel(playerObjects[0]);

        if (GameConstants.LOCAL_MODE) {
            OpponentSubmit.gameObject.SetActive(true);
            OpponentSubmit.onClick.AddListener(Interpreter.SubmitActions);
        }
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
            SetRobotPanel(opponentPlayer.team[i], true);
        }
    }

    void SetUsersPlayerPanel(Game.Player userPlayer)
    {
        userNameText.SetText(userPlayer.name);
        for (int i = 0; i < userPlayer.team.Length; i++)
        {
            SetRobotPanel(userPlayer.team[i], false);
        }
    }

    public GameObject SetRobotPanel(Robot r, bool isOpponent)
    {
        GameObject panel = Instantiate(RobotPanel, isOpponent ? OpponentsRobots.transform : UsersRobots.transform);
        panel.name = "Robot" + r.id;
        Transform icon = panel.transform.GetChild(1);
        icon.GetComponentInChildren<Image>().sprite = Array.Find(sprites, (Sprite s) => s.name.Equals(r.name));
        TMP_Text[] fields = panel.GetComponentsInChildren<TMP_Text>();
        fields[0].SetText(r.name);
        fields[1].SetText(0.ToString());
        for (int i = GameConstants.MAX_PRIORITY; i > 0; i--)
        {
            CommandSlotController cmd = Instantiate(CommandSlot, panel.transform.GetChild(3));
            cmd.Initialize(r.id, i, r.priority);
            cmd.isOpponent = isOpponent;
        }
        robotIdToPanel[r.id] = panel;
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
        bool clickable = true;
        for (int i = 0; i < panel.childCount; i++)
        {
            CommandSlotController child = panel.GetChild(i).GetComponent<CommandSlotController>();
            child.deletable = false;
            child.Arrow.sprite = null;
            if (!child.Closed())
            {
                child.Open();
                child.Clickable = clickable;
                clickable = false;
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
            CommandSlotController cmd = commandPanel.GetChild(commandPanel.childCount - p).GetComponent<CommandSlotController>();
            if (cmd.Arrow.sprite != null && cmd.Arrow.sprite.name.StartsWith(Command.GetDisplay(cmd.GetType())))
            {
                cmd.Highlight();
            }
        }
    }

    public void ColorCommandsSubmitted(short id)
    {
        Transform panel = robotIdToPanel[id].transform.GetChild(3);
        for (int i = 0; i < panel.childCount; i++)
        {
            CommandSlotController cmd = panel.GetChild(i).GetComponent<CommandSlotController>();
            if (!cmd.Closed()) cmd.Submit();
        }
        panel.parent.GetComponentsInChildren<TMP_Text>()[1].SetText(0.ToString());
    }

    public void addSubmittedCommand(Command cmd, short id)
    {
        Transform panel = robotIdToPanel[id].transform.GetChild(3);
        int powerConsumed = 0;
        for (int i = 0; i < panel.childCount; i++)
        {
            CommandSlotController child = panel.GetChild(i).GetComponent<CommandSlotController>();
            if (!child.Closed() && child.Arrow.sprite == null)
            {
                if (!child.Closed()) child.Open();
                if (cmd is Command.Spawn)
                {
                    child.Arrow.sprite = Interpreter.boardController.tile.queueSprites[cmd.direction];
                } else
                {
                    child.Arrow.sprite = GetArrow(cmd.ToSpriteString());
                    child.Arrow.rectTransform.localRotation = Quaternion.Euler(Vector3.forward * cmd.direction * 90);
                }
                child.deletable = child.Opened();
                child.Clickable = false;
                if (i + 1 < panel.childCount)
                {
                    panel.GetChild(i + 1).GetComponent<CommandSlotController>().Clickable = true;
                }
                powerConsumed += Command.power[cmd.GetType()];
                break;
            } else if (child.Arrow.sprite != null)
            {
                if (child.Arrow.sprite.name.StartsWith(Command.Spawn.DISPLAY)) powerConsumed += Command.power[typeof(Command.Spawn)];
                else if (child.Arrow.sprite.name.StartsWith(Command.Move.DISPLAY)) powerConsumed += Command.power[typeof(Command.Move)];
                else if (child.Arrow.sprite.name.StartsWith(Command.Attack.DISPLAY)) powerConsumed += Command.power[typeof(Command.Attack)];
            }
        }
        panel.parent.GetComponentsInChildren<TMP_Text>()[1].SetText(powerConsumed.ToString());
    }

    public Tuple<string, byte>[] getCommandsSerialized(short id)
    {
        Transform panel = robotIdToPanel[id].transform.GetChild(3);
        List<Tuple<string, byte>> content = new List<Tuple<string, byte>>();
        for (int i = 0; i < panel.childCount; i++)
        {
            CommandSlotController child = panel.GetChild(i).GetComponent<CommandSlotController>();
            if (child.Closed()) continue;
            if (child.Arrow.sprite == null) break;
            string name = child.Arrow.sprite.name;
            byte d = name.StartsWith(Command.Spawn.DISPLAY) ? 
                (byte)Interpreter.boardController.tile.queueSprites.ToList().IndexOf(child.Arrow.sprite) : 
                (byte)(child.Arrow.rectTransform.localRotation.eulerAngles.z / 90);
            content.Add(new Tuple<string, byte>(child.Arrow.sprite.name, d));
        }
        return content.ToArray();
    }

    internal void DestroyCommandMenu()
    {
        foreach (short id in robotIdToPanel.Keys)
        {
            Transform commandPanel = robotIdToPanel[id].transform.GetChild(3);
            for (int i = 0; i < commandPanel.childCount; i++)
            {
                CommandSlotController child = commandPanel.GetChild(i).GetComponent<CommandSlotController>();
                child.Arrow.gameObject.SetActive(true);
                child.Menu.gameObject.SetActive(false);
                child.Submenu.gameObject.SetActive(false);
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
        float xMin = UsersRobots.transform.parent.GetComponent<RectTransform>().anchorMax.x;
        float xMax = OpponentsRobots.transform.parent.GetComponent<RectTransform>().anchorMin.x;
        boardCamera.rect = new Rect(xMin, 0, xMax - xMin, 1);
        boardCamera.transform.localPosition = new Vector3(Interpreter.boardController.boardCellsWide-1, Interpreter.boardController.boardCellsHeight-1,-20)/2;
        int iterations = 0;
        float diff;
        while (iterations < 20)
        {
            diff = (boardCamera.ViewportToWorldPoint(Vector3.back * boardCamera.transform.position.z).x + 0.5f);
            if (diff == 0) break;
            boardCamera.transform.position -= Vector3.forward * diff;
            iterations++;
        }
        float yspace = boardCamera.ViewportToWorldPoint(new Vector3(0, 1, -boardCamera.transform.position.z)).y - boardCamera.ViewportToWorldPoint(Vector3.back * boardCamera.transform.position.z).y;
        float minusHeight = 1 - (yspace - Interpreter.boardController.boardCellsHeight) / 2;
        Interpreter.boardController.primaryDock.transform.localScale -= Vector3.up*minusHeight;
        Interpreter.boardController.secondaryDock.transform.localScale -= Vector3.up * minusHeight;
        Interpreter.boardController.primaryDock.transform.position += Vector3.up * (minusHeight / 2);
        Interpreter.boardController.secondaryDock.transform.position += Vector3.down * (minusHeight / 2);
        Array.ForEach(Interpreter.robotControllers.Values.ToArray(), (RobotController r) =>
        {
            Vector3 oldScale = r.transform.localScale;
            oldScale.x = ((1 - minusHeight) * oldScale.y) / r.transform.parent.localScale.x;
            r.transform.localScale = oldScale;
        });
        if (!isPrimary)
        {
            Color tmp = Interpreter.boardController.primaryDock.color;
            Interpreter.boardController.primaryDock.color = Interpreter.boardController.secondaryDock.color;
            Interpreter.boardController.secondaryDock.color = tmp;
            Interpreter.Flip();
        }
    }

    public void Flip()
    {
        boardCamera.transform.Rotate(new Vector3(0, 0, 180));
        Interpreter.boardController.allQueueLocations.ToList().ForEach((TileController t) =>
        {
            //t.GetComponent<SpriteRenderer>().flipY = !t.GetComponent<SpriteRenderer>().flipY;
            //t.GetComponent<SpriteRenderer>().flipX = !t.GetComponent<SpriteRenderer>().flipX;
        });
        //Interpreter.boardController.primaryBatteryLocation.GetComponent<SpriteRenderer>().flipX = !Interpreter.boardController.primaryBatteryLocation.GetComponent<SpriteRenderer>().flipX;
        //Interpreter.boardController.primaryBatteryLocation.GetComponent<SpriteRenderer>().flipY = !Interpreter.boardController.primaryBatteryLocation.GetComponent<SpriteRenderer>().flipY;
        //Interpreter.boardController.secondaryBatteryLocation.GetComponent<SpriteRenderer>().flipX = !Interpreter.boardController.secondaryBatteryLocation.GetComponent<SpriteRenderer>().flipX;
        //Interpreter.boardController.secondaryBatteryLocation.GetComponent<SpriteRenderer>().flipY = !Interpreter.boardController.secondaryBatteryLocation.GetComponent<SpriteRenderer>().flipY;
        userScore.transform.Rotate(Vector3.forward, 180);
        opponentScore.transform.Rotate(Vector3.forward, 180);
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

    public Sprite GetArrow(string eventName)
    {
        return Array.Find(arrows, (Sprite s) => s.name.Equals(eventName));
    }
}
