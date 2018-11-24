using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UIController : Controller
{
	public Image opponentBackground;
    public RobotPanelsContainerController opponentsRobots;
    public TMP_Text opponentsPlayerName;
    public Image myBackground;
    public RobotPanelsContainerController myRobots;
    public TMP_Text myPlayerName;
    public ButtonContainerController robotButtonContainer;
    public ButtonContainerController commandButtonContainer;
    public ButtonContainerController directionButtonContainer;
    public ButtonContainerController actionButtonContainer;
    public LayerMask boardLayer;
    public LayerMask selectedLayer;
    public MenuItemController submitCommands;
    public MenuItemController stepBackButton;
    public MenuItemController stepForwardButton;
    public MenuItemController backToPresent;
    public MenuItemController genericButton;
    public Sprite[] arrows;
    public Sprite[] queueSprites;
    public StatsController statsInterface;
    public StatusModalController statusText;

    private RobotController selectedRobotController;
    // public Image SplashScreen;

    void Start()
    {
        BaseGameManager.InitializeUI(this);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.B))
        {
            BackToSetup();
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
        MenuItemController[] mics = robotButtonContainer.GetComponentsInChildren<MenuItemController>();
        for (int i = 0; i < mics.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) mics[i].Click();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            commandButtonContainer.GetComponentsInChildren<MenuItemController>()[0].Click();
        } else if (Input.GetKeyDown(KeyCode.M))
        {
            commandButtonContainer.GetComponentsInChildren<MenuItemController>()[1].Click();
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            commandButtonContainer.GetComponentsInChildren<MenuItemController>()[2].Click();
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            directionButtonContainer.GetComponentsInChildren<MenuItemController>()[0].Click();
        } else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            directionButtonContainer.GetComponentsInChildren<MenuItemController>()[1].Click();
        } else if(Input.GetKeyDown(KeyCode.DownArrow))
        {
            directionButtonContainer.GetComponentsInChildren<MenuItemController>()[2].Click();
        } else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            directionButtonContainer.GetComponentsInChildren<MenuItemController>()[3].Click();
        }
        if (Input.GetKeyDown(KeyCode.Return))
        {
            submitCommands.Click();
        }
    }

    public void BackToSetup()
    {
        SceneManager.LoadScene("Setup");
    }

    public void InitializeUI(Game.Player myPlayer, Game.Player opponentPlayer)
    {
        SetOpponentPlayerPanel(opponentPlayer);
        SetMyPlayerPanel(myPlayer);

        submitCommands.Deactivate();
        backToPresent.Deactivate();
        stepBackButton.Deactivate();
        stepForwardButton.Deactivate();

        robotButtonContainer.SetButtons(true);
        commandButtonContainer.SetButtons(false);
        directionButtonContainer.SetButtons(false);
    }

    private void SetOpponentPlayerPanel(Game.Player player)
    {
        opponentsPlayerName.text = player.name;
        SetPlayerPanel(player, opponentsRobots);
    }

    private void SetMyPlayerPanel(Game.Player player)
    {
        myPlayerName.text = player.name;
        SetPlayerPanel(player, myRobots);
    }

    void SetPlayerPanel(Game.Player player, RobotPanelsContainerController container)
    {
        container.Initialize(player.team.Length);
        Util.ForEach(player.team, r => container.AddPanel(r));
    }

    public void BindUiToRobotController(short robotId, RobotController robotController)
    {
        MenuItemController robotButton = Instantiate(genericButton, robotButtonContainer.transform);
        RobotPanelsContainerController container = robotController.isOpponent ? opponentsRobots : myRobots;
        robotButton.SetSprite(container.GetSprite(robotId));
        robotButton.SetCallback(() => RobotButtonCallback(robotButton, robotController, robotId));
        robotButton.gameObject.SetActive(!robotController.isOpponent);
        robotButtonContainer.AddRobotButton(robotButton);

        container.BindCommandClickCallback(robotController, CommandSlotClickCallback);
    }

    private void RobotButtonCallback(MenuItemController robotButton, RobotController robotController, short robotId)
    {
        RobotButtonSelect(robotButton, robotController);
        commandButtonContainer.EachMenuItemSet(c => CommandButtonCallback(c, robotButton, robotController));
    }

    private void RobotButtonSelect(MenuItemController robotButton, RobotController robotController)
    {
        robotButtonContainer.SetButtons(true);
        ChangeLayer(selectedRobotController, boardLayer);
        robotButton.Select();
        ChangeLayer(robotController, selectedLayer);
        selectedRobotController = robotController;
        robotController.ShowMenuOptions(commandButtonContainer);
        directionButtonContainer.SetButtons(false);
        directionButtonContainer.ClearSprites();
    }

    private void CommandButtonCallback(MenuItemController commandButton, MenuItemController robotButton, RobotController robotController)
    {
        commandButtonContainer.SetSelected(commandButton);
        directionButtonContainer.SetButtons(true);
        directionButtonContainer.EachMenuItem(d => EachDirectionButton(d, robotButton, robotController, commandButton.name));
    }

    private void EachDirectionButton(MenuItemController directionButton, MenuItemController robotButton, RobotController robotController, string commandName)
    {
        bool isSpawn = commandName.Equals(Command.GetDisplay(Command.SPAWN_COMMAND_ID));
        byte dir = (byte) Util.FindIndex(Command.byteToDirectionString, s => s.Equals(directionButton.name));
        directionButton.SetSprite(isSpawn ? queueSprites[dir] : GetArrow(commandName + " Arrow"));
        directionButton.SetCallback(() => DirectionButtonCallback(robotButton, robotController, commandName, dir));
        directionButton.GetComponentInChildren<SpriteRenderer>().transform.localRotation = Quaternion.Euler(Vector3.up * 180 + (isSpawn ? Vector3.zero : Vector3.forward * dir * 90));
        directionButton.GetComponentInChildren<SpriteRenderer>().color = isSpawn ? Color.gray : Color.white;
    }

    private void DirectionButtonCallback(MenuItemController robotButton, RobotController robotController, string commandName, byte dir)
    {
        robotController.AddRobotCommand(commandName, dir, AddSubmittedCommand);
        RobotButtonSelect(robotButton, robotController);
    }

    private void CommandSlotClickCallback(RobotController r, int index)
    {
        ClearCommands(r.id);
        if (r.commands[index] is Command.Spawn)
        {
            r.commands = new Command[0];
        }
        else
        {
            r.commands = Util.RemoveAt(r.commands, index);
            Util.ForEach(r.commands, c => AddSubmittedCommand(c, r.id));
        }

        robotButtonContainer.SetButtons(true);
        commandButtonContainer.SetButtons(false);
        directionButtonContainer.SetButtons(false);
        directionButtonContainer.EachMenuItem(m => m.ClearSprite());
    }

    public void ClearCommands(short id)
    {
        myRobots.ClearCommands(id);
    }

    public void HighlightCommands(byte commandId, byte p)
    {
        myRobots.HighlightCommands(commandId, p);
    }

    public void ColorCommandsSubmitted(short id)
    {
        myRobots.ColorCommandsSubmitted(id);
    }

    public void AddSubmittedCommand(Command cmd, short id)
    {
        Sprite s = cmd is Command.Spawn ? queueSprites[cmd.direction] : GetArrow(cmd.ToString());
        myRobots.AddSubmittedCommand(cmd, id, s);
        submitCommands.Activate();
    }

    internal void DestroyCommandMenu()
    {
        myRobots.DestroyCommandMenu();
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
        return Util.Find(arrows, (Sprite s) => s.name.Equals(eventName));
    }

    public void Splash(bool win)
    {
        //TODO: Add Calvin's screens
        //SplashScreen.GetComponentInChildren<Text>().text = win ? "YOU WIN!" : "YOU LOSE!";
        //SplashScreen.gameObject.SetActive(true);
        //SetButtons(false);
    }

    public void ChangeToBoardLayer(RobotController r)
    {
        ChangeLayer(r, boardLayer);
    }

    private void ChangeLayer(RobotController r, int l)
    {
        if (r != null) ChangeLayer(r.gameObject, l);
    }

    private void ChangeLayer(GameObject g, int l)
    {
        if (g.layer == l) return;
        g.layer = l;
        Util.ForEach(g.transform.childCount, i => ChangeLayer(g.transform.GetChild(i).gameObject, l));
    }

}
