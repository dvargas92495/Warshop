using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;

public class UIController : MonoBehaviour {

	public TextMesh ScoreModel;
    
    internal TextMesh opponentScore;
    public GameObject OpponentsRobots;
    
    internal TextMesh userScore;
    public GameObject UsersRobots;
    public GameObject RobotPanel;
    public CommandSlotController CommandSlot;
   // public GameObject priorityArrow;

   // public Button OpponentSubmit;
   // public Button SubmitCommands;
   // public Button StepBackButton;
   // public Button StepForwardButton;
   // public Button BackToPresent;
   // public Image SplashScreen;
    
    public Sprite[] sprites;
    public Sprite[] arrows;
    public Camera cam;

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
        cam.transform.position += Vector3.forward * Interpreter.boardController.cam.transform.position.z;
        SetPlayerPanel(playerObjects[1], true);
        SetPlayerPanel(playerObjects[0], false);

        userScore = Instantiate(ScoreModel, Interpreter.boardController.GetVoidTile(isPrimary).transform);
        userScore.GetComponent<MeshRenderer>().sortingOrder = 1;
        opponentScore = Instantiate(ScoreModel, Interpreter.boardController.GetVoidTile(!isPrimary).transform);
        opponentScore.GetComponent<MeshRenderer>().sortingOrder = 1;
        SetBattery(playerObjects[0].battery, playerObjects[1].battery);
        if (!isPrimary)
        {
            Interpreter.boardController.cam.transform.Rotate(new Vector3(0, 0, 180));
            Interpreter.boardController.allQueueLocations.ToList().ForEach((TileController t) =>
            {
                //t.GetComponent<SpriteRenderer>().flipY = !t.GetComponent<SpriteRenderer>().flipY;
                //t.GetComponent<SpriteRenderer>().flipX = !t.GetComponent<SpriteRenderer>().flipX;
            });
            userScore.transform.Rotate(Vector3.forward, 180);
            opponentScore.transform.Rotate(Vector3.forward, 180);
        }
        return;

        /*if (GameConstants.LOCAL_MODE) {
            OpponentSubmit.gameObject.SetActive(true);
            OpponentSubmit.onClick.AddListener(Interpreter.SubmitActions);
        }
        SubmitCommands.onClick.AddListener(Interpreter.SubmitActions);
        BackToPresent.onClick.AddListener(Interpreter.BackToPresent);
        StepBackButton.onClick.AddListener(Interpreter.StepBackward);
        StepForwardButton.onClick.AddListener(Interpreter.StepForward);
        */
        
    }

    void SetPlayerPanel(Game.Player player, bool isOpponent)
    {
        Transform container = (isOpponent ? OpponentsRobots.transform : UsersRobots.transform);

        float depth = 1;
        Vector3 topLeft = cam.ViewportToWorldPoint(new Vector3(0, 1, depth));
        Vector3 bottomRight = cam.ViewportToWorldPoint(new Vector3(0.2f, 0, depth));
        container.parent.localScale = new Vector3(bottomRight.x - topLeft.x, topLeft.y - bottomRight.y, 1);
        container.parent.position = new Vector3((bottomRight.x + topLeft.x) / (isOpponent ? -2 : 2), (topLeft.y + bottomRight.y) / 2, cam.transform.position.z - depth);

        TextMesh playerName = container.parent.GetComponentInChildren<TextMesh>();
        playerName.text = player.name;

        for (int i = 0; i < player.team.Length; i++)
        {
            Robot r = player.team[i];
            GameObject panel = Instantiate(RobotPanel, container);
            panel.name = "Robot" + r.id;
            panel.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = Array.Find(sprites, (Sprite s) => s.name.Equals(r.name));
            TextMesh[] fields = panel.GetComponentsInChildren<TextMesh>();
            fields[0].text = r.name;
            fields[1].text = 0.ToString();
            fields[0].GetComponent<MeshRenderer>().sortingOrder = fields[1].GetComponent<MeshRenderer>().sortingOrder = 2;
            for (int c = GameConstants.MAX_PRIORITY; c > 0; c--)
            {
                CommandSlotController cmd = Instantiate(CommandSlot, panel.transform.GetChild(3));
                cmd.Initialize(r.id, c, r.priority);
                cmd.isOpponent = isOpponent;
                cmd.transform.localScale = new Vector3(1, 1.0f / GameConstants.MAX_PRIORITY, 1);
                cmd.transform.localPosition = new Vector3(0, (1.0f / GameConstants.MAX_PRIORITY) * (c + 0.5f) - 0.5f, 0);
            }
            robotIdToPanel[r.id] = panel;
            panel.transform.localScale = new Vector3(1.0f / player.team.Length, 1, 1);
            panel.transform.localPosition = new Vector3((1.0f / player.team.Length)*(i+0.5f) - 0.5f, 0, 0);
        }
    }

    public void SetPriority(int priority)
    {
        /*if (priority == -1)
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
        priorityArrow.SetActive(true);*/
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
                if (clickable) child.Next();
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
        panel.parent.GetComponentsInChildren<TextMesh>()[1].text = 0.ToString();
    }

    public void addSubmittedCommand(Command cmd, short id)
    {
        Transform panel = robotIdToPanel[id].transform.GetChild(3);
        int powerConsumed = 0;
        for (int i = 0; i < panel.childCount; i++)
        {
            CommandSlotController child = panel.GetChild(i).GetComponent<CommandSlotController>();
            if (child.IsNext())
            {
                child.Open();
                if (cmd is Command.Spawn)
                {
                    child.Arrow.sprite = Interpreter.boardController.tile.queueSprites[cmd.direction];
                } else
                {
                    child.Arrow.sprite = GetArrow(cmd.ToSpriteString());
                    child.Arrow.transform.localRotation = Quaternion.Euler(Vector3.forward * cmd.direction * 90);
                }
                child.deletable = true;
                if (i + 1 < panel.childCount)
                {
                    panel.GetChild(i + 1).GetComponent<CommandSlotController>().Next();
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
        panel.parent.GetComponentsInChildren<TextMesh>()[1].text = powerConsumed.ToString();
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
                (byte)(child.Arrow.transform.localRotation.eulerAngles.z / 90);
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

    public void SetButtons(bool b)
    {
        //StepBackButton.interactable = StepForwardButton.interactable = BackToPresent.interactable = SubmitCommands.interactable = b;
    }

    public void SetSubmitButton(bool b)
    {
        //SubmitCommands.interactable = b;
    }

    public void LightUpPanel(bool bright, bool isUser)
    {
        SpriteRenderer panel = (isUser ? UsersRobots : OpponentsRobots).transform.parent.GetComponent<SpriteRenderer>();
        Color regular = (isUser ? new Color(0, 0.5f, 1.0f, 1.0f) : new Color(1.0f, 0, 0, 1.0f));
        float mult = (bright ? 1.0f : 0.5f);
        panel.color = new Color(regular.r * mult, regular.g*mult, regular.b * mult, regular.a * mult);
    }

    public Sprite GetArrow(string eventName)
    {
        return Array.Find(arrows, (Sprite s) => s.name.Equals(eventName));
    }

    public void Splash(bool win)
    {
        //SplashScreen.GetComponentInChildren<Text>().text = win ? "YOU WIN!" : "YOU LOSE!";
        //SplashScreen.gameObject.SetActive(true);
        //SetButtons(false);
    }
}
