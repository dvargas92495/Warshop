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
    
    internal TextMesh opponentScore;
    public Image OpponentBackground;
    public GameObject OpponentsRobots;
    
    internal TextMesh userScore;
    public Image UserBackground;
    public GameObject UsersRobots;

    public GameObject RobotPanel;
    public CommandSlotController CommandSlot;
    public GameObject Controls;
   // public GameObject priorityArrow;

    public MenuItemController SubmitCommands;
    public MenuItemController StepBackButton;
    public MenuItemController StepForwardButton;
    public MenuItemController BackToPresent;
    public MenuItemController GenericButton;
    public GameObject RobotButtonContainer;
    public GameObject CommandButtonContainer;
    public TMP_Text UserPlayerName;
    public TMP_Text OpponentsPlayerName;
    public GameObject DirectionButtonContainer;
    // public Image SplashScreen;

    public Sprite Default;
    public Sprite[] sprites;
    public Sprite[] arrows;    

    private Dictionary<short, GameObject> robotIdToPanel = new Dictionary<short, GameObject>();
    private int CommandChildIndex = 3;
    internal int BoardLayer = 11;
    private int SelectedLayer = 12;

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
        MenuItemController[] mics = RobotButtonContainer.GetComponentsInChildren<MenuItemController>();
        for (int i = 0; i < mics.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) mics[i].Click();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            CommandButtonContainer.GetComponentsInChildren<MenuItemController>()[0].Click();
        } else if (Input.GetKeyDown(KeyCode.M))
        {
            CommandButtonContainer.GetComponentsInChildren<MenuItemController>()[1].Click();
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            CommandButtonContainer.GetComponentsInChildren<MenuItemController>()[2].Click();
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            DirectionButtonContainer.GetComponentsInChildren<MenuItemController>()[0].Click();
        } else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            DirectionButtonContainer.GetComponentsInChildren<MenuItemController>()[1].Click();
        } else if(Input.GetKeyDown(KeyCode.DownArrow))
        {
            DirectionButtonContainer.GetComponentsInChildren<MenuItemController>()[2].Click();
        } else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            DirectionButtonContainer.GetComponentsInChildren<MenuItemController>()[3].Click();
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
        SetPlayerPanel(playerObjects[1], true);
        SetPlayerPanel(playerObjects[0], false);

        //userScore = Instantiate(ScoreModel, Interpreter.boardController.GetVoidTile(isPrimary).transform);
        userScore = Instantiate(ScoreModel, (isPrimary ? Interpreter.boardController.primaryBatteryLocation : 
            Interpreter.boardController.secondaryBatteryLocation).transform);
        userScore.GetComponent<MeshRenderer>().sortingOrder = 1;
        //opponentScore = Instantiate(ScoreModel, Interpreter.boardController.GetVoidTile(!isPrimary).transform);
        opponentScore = Instantiate(ScoreModel, (!isPrimary ? Interpreter.boardController.primaryBatteryLocation : 
            Interpreter.boardController.secondaryBatteryLocation).transform);
        opponentScore.GetComponent<MeshRenderer>().sortingOrder = 1;
        SetBattery(playerObjects[0].battery, playerObjects[1].battery);
        Interpreter.boardController.ColorQueueBelt(isPrimary);
        if (!isPrimary)
        {
            Interpreter.boardController.cam.transform.Rotate(new Vector3(60, 0, 180));
            Interpreter.boardController.cam.transform.position += Vector3.up * 16; //TODO: Magic Number?
            Interpreter.boardController.allQueueLocations.ToList().ForEach((TileController t) =>
            {
                TMP_Text s = t.transform.GetComponentInChildren<TMP_Text>();
                s.transform.Rotate(new Vector3(0, 0, 180));
                s.fontSharedMaterial = s.fontSharedMaterial.Equals(Interpreter.boardController.tile.userSpawnTileTextMaterial) ?
                    Interpreter.boardController.tile.opponentSpawnTileTextMaterial : 
                    Interpreter.boardController.tile.userSpawnTileTextMaterial;
            });
            userScore.transform.Rotate(Vector3.forward, 180);
            opponentScore.transform.Rotate(Vector3.forward, 180);
        }
        SubmitCommands.SetCallback(Interpreter.SubmitActions);
        BackToPresent.SetCallback(Interpreter.BackToPresent);
        StepBackButton.SetCallback(Interpreter.StepBackward);
        StepForwardButton.SetCallback(Interpreter.StepForward);
        SubmitCommands.Activate();
        BackToPresent.Deactivate();
        StepBackButton.Deactivate();
        StepForwardButton.Deactivate();
        SetButtons(RobotButtonContainer, true);
        SetButtons(CommandButtonContainer, false);
        SetButtons(DirectionButtonContainer, false);
        return;
        
    }

    void SetPlayerPanel(Game.Player player, bool isOpponent)
    {
        Transform container = (isOpponent ? OpponentsRobots.transform : UsersRobots.transform);
        
        TMP_Text playerName = (isOpponent ? OpponentsPlayerName : UserPlayerName);
        playerName.text = player.name;

        for (int i = 0; i < player.team.Length; i++)
        {
            //Robot Panel First
            Robot r = player.team[i];
            GameObject panel = Instantiate(RobotPanel, container);
            panel.name = "Robot" + r.id;
            Sprite robotSprite = Array.Find(sprites, (Sprite s) => s.name.Equals(r.name));
            panel.transform.GetChild(1).GetComponent<SpriteRenderer>().sprite = robotSprite;
            TextMesh field = panel.GetComponentInChildren<TextMesh>();
            field.text = 0.ToString();
            field.GetComponent<MeshRenderer>().sortingOrder = 2;
            for (int c = GameConstants.MAX_PRIORITY; c > 0; c--)
            {
                CommandSlotController cmd = Instantiate(CommandSlot, panel.transform.GetChild(CommandChildIndex));
                cmd.Initialize(r.id, c, r.priority);
                cmd.isOpponent = isOpponent;
                cmd.transform.localScale = new Vector3(1, 1.0f / GameConstants.MAX_PRIORITY, 1);
                cmd.transform.localPosition = new Vector3(0, (1.0f / GameConstants.MAX_PRIORITY) * (c + 0.5f) - 0.5f, 0);
            }
            robotIdToPanel[r.id] = panel;
            panel.transform.localPosition = new Vector3((1.0f / player.team.Length)*(i+0.5f) - 0.5f, 0, 0);

            //Then Command Button - Beware some unreadable code below
            MenuItemController robotButton = Instantiate(GenericButton, RobotButtonContainer.transform);
            robotButton.GetComponentInChildren<SpriteRenderer>().sprite = robotSprite;
            robotButton.SetCallback(() => {
                Action robotButtonSelect = () =>
                {
                    SetButtons(RobotButtonContainer, true);
                    Interpreter.robotControllers.Values.ToList().ForEach((RobotController otherR) => Util.ChangeLayer(otherR.gameObject, BoardLayer));
                    robotButton.Select();
                    Util.ChangeLayer(Interpreter.robotControllers[r.id].gameObject, SelectedLayer);
                    Interpreter.robotControllers[r.id].ShowMenuOptions(CommandButtonContainer);
                };
                robotButtonSelect();
                SetButtons(DirectionButtonContainer, false);
                EachMenuItem(DirectionButtonContainer, (MenuItemController m) => m.GetComponentInChildren<SpriteRenderer>().sprite = null);
                EachMenuItemSet(CommandButtonContainer, (string m) => {
                    EachMenuItem(CommandButtonContainer, (MenuItemController d) => {
                        if (d.IsSelected() && !d.name.Equals(m)) d.Activate();
                    });
                    SetButtons(DirectionButtonContainer, true);
                    bool isSpawn = m.Equals(Command.Spawn.DISPLAY);
                    EachMenuItem(DirectionButtonContainer, (MenuItemController d) => {
                        byte dir = Command.byteToDirectionString.First((KeyValuePair<byte, string> pair) => pair.Value.Equals(d.name)).Key;
                        d.GetComponentInChildren<SpriteRenderer>().sprite = isSpawn ?
                            Interpreter.boardController.tile.queueSprites[dir] : Interpreter.uiController.GetArrow(m + " Arrow");
                        d.SetCallback(() => {
                            Interpreter.robotControllers[r.id].addRobotCommand(m, dir);
                            EachMenuItem(DirectionButtonContainer, (MenuItemController d2) => d2.GetComponentInChildren<SpriteRenderer>().sprite = null);
                            robotButtonSelect();
                            SetButtons(DirectionButtonContainer, false);
                        });
                        d.GetComponentInChildren<SpriteRenderer>().transform.localRotation = Quaternion.Euler(Vector3.up*180 + (isSpawn ? Vector3.zero : Vector3.forward * dir * 90));
                        d.GetComponentInChildren<SpriteRenderer>().color = isSpawn ? Color.gray : Color.white; //TODO: Hack
                    });
                });
            });
            robotButton.gameObject.SetActive(!isOpponent);
            robotButton.transform.localPosition = Vector3.right*((i%4)*3 - 4.5f);
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
            child.Arrow.sprite = Default;
            if (!child.Closed())
            {
                child.Open();
                if (clickable) child.Next();
                clickable = false;
            }
        }
        panel.parent.GetComponentInChildren<TextMesh>().text = 0.ToString();
    }

    public void ClearCommands(short id)
    {
        ClearCommands(robotIdToPanel[id].transform.GetChild(CommandChildIndex));
    }

    public void HighlightCommands(Type t, byte p)
    {
        foreach (short id in robotIdToPanel.Keys)
        {
            Transform commandPanel = robotIdToPanel[id].transform.GetChild(CommandChildIndex);
            CommandSlotController cmd = commandPanel.GetChild(commandPanel.childCount - p).GetComponent<CommandSlotController>();
            if (cmd.Arrow.sprite.name.StartsWith(Command.GetDisplay(t)))
            {
                cmd.Highlight();
            }
        }
    }

    public void ColorCommandsSubmitted(short id)
    {
        Transform panel = robotIdToPanel[id].transform.GetChild(CommandChildIndex);
        for (int i = 0; i < panel.childCount; i++)
        {
            CommandSlotController cmd = panel.GetChild(i).GetComponent<CommandSlotController>();
            if (!cmd.Closed()) cmd.Submit();
        }
    }

    public void addSubmittedCommand(Command cmd, short id)
    {
        Transform panel = robotIdToPanel[id].transform.GetChild(CommandChildIndex);
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
                    child.Arrow.transform.localRotation = Quaternion.identity;
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
            } else if (child.Arrow.sprite.name.StartsWith(Command.Spawn.DISPLAY)) powerConsumed += Command.power[typeof(Command.Spawn)];
              else if (child.Arrow.sprite.name.StartsWith(Command.Move.DISPLAY)) powerConsumed += Command.power[typeof(Command.Move)];
              else if (child.Arrow.sprite.name.StartsWith(Command.Attack.DISPLAY)) powerConsumed += Command.power[typeof(Command.Attack)];
        }
        panel.parent.GetComponentInChildren<TextMesh>().text = powerConsumed.ToString();
    }

    public Tuple<string, byte>[] getCommandsSerialized(short id)
    {
        Transform panel = robotIdToPanel[id].transform.GetChild(3);
        List<Tuple<string, byte>> content = new List<Tuple<string, byte>>();
        for (int i = 0; i < panel.childCount; i++)
        {
            CommandSlotController child = panel.GetChild(i).GetComponent<CommandSlotController>();
            if (child.Closed()) continue;
            if (child.Arrow.sprite.Equals(Default)) break;
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

    internal void EachMenuItem(GameObject g, Action<MenuItemController> a)
    {
        g.GetComponentsInChildren<MenuItemController>(true).ToList().ForEach(a);
    }

    private void EachMenuItemSet(GameObject g, Action<string> a)
    {
        EachMenuItem(g, (MenuItemController m) => {
            m.SetCallback(() => a(m.name));
        });
    }

    public void SetButtons(GameObject container, bool b)
    {
        EachMenuItem(container, (MenuItemController m) => m.SetActive(b));
    }

    public void SetButtons(bool b)
    {
        SetButtons(SubmitCommands.transform.parent.gameObject, b);
    }

    public void LightUpPanel(bool bright, bool isUser)
    {
        //Image panel = (isUser ? UserBackground : OpponentBackground);
        //Color regular = (isUser ? new Color(0, 0.5f, 1.0f, 1.0f) : new Color(1.0f, 0, 0, 1.0f));
        //float mult = (bright ? 1.0f : 0.5f);
        //panel.color = new Color(regular.r * mult, regular.g*mult, regular.b * mult, regular.a * mult);
        //TODO - Need another indicator of opponent ready here
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
