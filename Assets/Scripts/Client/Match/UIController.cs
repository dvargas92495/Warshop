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
    public LayerMask myPlayerLayer;
    public LayerMask opponentPlayerLayer;
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
        for (int i = 0; i < GameConstants.MAX_ROBOTS_ON_SQUAD; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) robotButtonContainer.Get(i).Click();
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            commandButtonContainer.GetByName(Command.GetDisplay(Command.SPAWN_COMMAND_ID)).Click();
        } else if (Input.GetKeyDown(KeyCode.M))
        {
            commandButtonContainer.GetByName(Command.GetDisplay(Command.MOVE_COMMAND_ID)).Click();
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            commandButtonContainer.GetByName(Command.GetDisplay(Command.ATTACK_COMMAND_ID)).Click();
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            directionButtonContainer.GetByName(Command.byteToDirectionString[Command.UP]).Click();
        } else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            directionButtonContainer.GetByName(Command.byteToDirectionString[Command.LEFT]).Click();
        } else if(Input.GetKeyDown(KeyCode.DownArrow))
        {
            directionButtonContainer.GetByName(Command.byteToDirectionString[Command.DOWN]).Click();
        } else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            directionButtonContainer.GetByName(Command.byteToDirectionString[Command.RIGHT]).Click();
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
        SetPlayerPanel(player, opponentsRobots, opponentPlayerLayer);
    }

    private void SetMyPlayerPanel(Game.Player player)
    {
        myPlayerName.text = player.name;
        SetPlayerPanel(player, myRobots, myPlayerLayer);
    }

    void SetPlayerPanel(Game.Player player, RobotPanelsContainerController container, LayerMask layer)
    {
        container.Initialize(player.team.GetLength());
        player.team.ForEach(container.AddPanel);
        ChangeLayer(container.gameObject, layer);
    }

    public void BindUiToRobotController(short robotId, RobotController robotController)
    {
        MenuItemController robotButton = Instantiate(genericButton, robotButtonContainer.transform);
        RobotPanelsContainerController container = robotController.isOpponent ? opponentsRobots : myRobots;
        robotButton.SetSprite(container.GetSprite(robotId));
        robotButton.SetCallback(() => RobotButtonCallback(robotButton, robotController, robotId));
        robotButton.gameObject.SetActive(!robotController.isOpponent);
        int teamLength = Util.ToList(robotButtonContainer.menuItems).Count(m => m.gameObject.activeInHierarchy == !robotController.isOpponent);
        robotButton.transform.localPosition = Vector3.right * (teamLength % 4 * 3 - 4.5f);
        robotButtonContainer.menuItems = Util.Add(robotButtonContainer.menuItems, robotButton);

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
        byte dir = (byte) Util.ToList(Command.byteToDirectionString).FindIndex(directionButton.name);
        directionButton.SetSprite(isSpawn ? queueSprites[dir] : GetArrow(commandName));
        directionButton.SetCallback(() => DirectionButtonCallback(robotButton, robotController, commandName, dir));
        directionButton.spriteRenderer.transform.localRotation = Quaternion.Euler(Vector3.up * 180 + (isSpawn ? Vector3.zero : Vector3.forward * dir * 90));
        directionButton.spriteRenderer.color = isSpawn ? Color.gray : Color.white;
    }

    private void DirectionButtonCallback(MenuItemController robotButton, RobotController robotController, string commandName, byte dir)
    {
        robotController.AddRobotCommand(commandName, dir, AddSubmittedCommand);
        RobotButtonSelect(robotButton, robotController);
        commandButtonContainer.ClearSelected();
    }

    private void CommandSlotClickCallback(RobotController r, int index)
    {
        ClearCommands(r.id, r.isOpponent);
        if (r.commands.Get(index) is Command.Spawn)
        {
            r.commands.Clear();
        }
        else
        {
            r.commands.RemoveAt(index);
            r.commands.ForEach(c => AddSubmittedCommand(c, r.id, r.isOpponent));
        }

        robotButtonContainer.SetButtons(true);
        commandButtonContainer.SetButtons(false);
        directionButtonContainer.SetButtons(false);
        directionButtonContainer.EachMenuItem(m => m.ClearSprite());
    }

    public void ClearCommands(short id, bool isOpponent)
    {
        RobotPanelsContainerController robotPanelsContainer = isOpponent ? opponentsRobots : myRobots;
        robotPanelsContainer.ClearCommands(id);
    }

    public void HighlightCommands(byte p)
    {
        myRobots.HighlightCommands(p);
        opponentsRobots.HighlightCommands(p);
    }

    public void ColorCommandsSubmitted(short id, bool isOpponent)
    {
        RobotPanelsContainerController robotPanelsContainer = isOpponent ? opponentsRobots : myRobots;
        robotPanelsContainer.ColorCommandsSubmitted(id);
    }

    public void AddSubmittedCommand(Command cmd, short id, bool isOpponent)
    {
        Sprite s = cmd is Command.Spawn ? queueSprites[cmd.direction] : GetArrow(Command.GetDisplay(cmd.commandId));
        RobotPanelsContainerController robotPanelsContainer = isOpponent ? opponentsRobots : myRobots;
        robotPanelsContainer.AddSubmittedCommand(cmd, id, s);
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
        return Util.ToList(arrows).Find(s => s.name.Equals(eventName));
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
        ChangeLayerHelper(g, Math.Log2(l));
    }

    private void ChangeLayerHelper(GameObject g, int l)
    {
        g.layer = l;
        Util.ToIntList(g.transform.childCount).ForEach(i => ChangeLayerHelper(g.transform.GetChild(i).gameObject, l));
    }

}
