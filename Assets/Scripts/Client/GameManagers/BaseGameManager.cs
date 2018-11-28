using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public abstract class BaseGameManager
{
    private static BaseGameManager instance;

    protected BoardController boardController;
    protected SetupController setupController;
    protected Game.Player myPlayer;
    protected Game.Player opponentPlayer;
    protected GameClient gameClient;
    protected UIController uiController;
    protected Util.Dictionary<short, RobotController> robotControllers;

    protected byte turnNumber = 1;
    protected int currentHistoryIndex;
    protected HistoryState[] history = new HistoryState[0];
    protected Map board;

    private static Logger log = new Logger(typeof(BaseGameManager).ToString());

    public static void InitializeLocal()
    {
        instance = new LocalGameManager();
    }

    public static void InitializeStandard(string playerSessionId, string ipAddress, int port)
    {
        instance = new StandardGameManager(playerSessionId, ipAddress, port);
    }

    public static void InitializeSetup(SetupController sc)
    {
        instance.InitializeSetupImpl(sc);
    }

    protected void InitializeSetupImpl(SetupController sc)
    {
        setupController = sc;
    }

    public static void SendPlayerInfo(string[] myRobotNames, string username)
    {
        instance.SendPlayerInfoImpl(myRobotNames, username);
    }

    protected void SendPlayerInfoImpl(string[] myRobotNames, string username)
    {
        myPlayer = new Game.Player(new Robot[0], username);
    }

    internal void LoadBoard(Robot[] myTeam, Robot[] opponentTeam, string opponentName, Map b)
    {
        myPlayer.team = myTeam;
        opponentPlayer.team = opponentTeam;
        opponentPlayer.name = opponentName;
        board = b;
        SceneManager.LoadScene("Match");
    }

    public static void InitializeBoard(BoardController bc)
    {
        instance.InitializeBoardImpl(bc);
    }

    protected void InitializeBoardImpl(BoardController bc)
    {
        boardController = bc;
        boardController.InitializeBoard(board);
        boardController.SetBattery(myPlayer.battery, opponentPlayer.battery);
        InitializeRobots();
    }

    private void InitializeRobots()
    {
        robotControllers = new Util.Dictionary<short, RobotController>(GameConstants.MAX_ROBOTS_ON_SQUAD*2);
        InitializePlayerRobots(myPlayer, boardController.myDock);
        InitializePlayerRobots(opponentPlayer, boardController.opponentDock);
    }

    private void InitializePlayerRobots(Game.Player player, DockController dock)
    {
        Util.ForEach(player.team, r => InitializeRobot(r, dock));
    }

    private void InitializeRobot(Robot robot, DockController dock)
    {
        RobotController r = boardController.LoadRobot(robot, dock.transform);
        r.isOpponent = dock.Equals(boardController.opponentDock);
        robotControllers.Add(r.id, r);
        r.transform.localPosition = dock.PlaceInBelt();
    }

    public static void InitializeUI(UIController ui)
    {
        instance.InitializeUiImpl(ui);
    }

    protected void InitializeUiImpl(UIController ui)
    {
        uiController = ui;
        uiController.InitializeUI(myPlayer, opponentPlayer);
        robotControllers.ForEach(uiController.BindUiToRobotController);
        uiController.submitCommands.SetCallback(SubmitCommands);
        uiController.backToPresent.SetCallback(BackToPresent);
        uiController.stepForwardButton.SetCallback(StepForward);
        uiController.stepBackButton.SetCallback(StepBackward);
        history = Util.Add(history, SerializeState(1, GameConstants.MAX_PRIORITY, 0));
    }

    protected abstract void SubmitCommands();

    protected Command[] GetSubmittedCommands()
    {
        uiController.LightUpPanel(true, true);
        Command[] commands = robotControllers.ReduceEachValue(new Command[0], AddCommands);
        uiController.commandButtonContainer.SetButtons(false);
        uiController.directionButtonContainer.SetButtons(false);
        uiController.submitCommands.Deactivate();
        return commands;
    }

    private Command[] AddCommands(Command[] commands, RobotController robot)
    {
        Util.ForEach(robot.commands, c => c.robotId = robot.id);
        Command[] returnCommands = Util.Add(commands, robot.commands);
        uiController.ColorCommandsSubmitted(robot.id);
        uiController.ChangeToBoardLayer(robot);
        return returnCommands;
    }

    protected void PlayEvents(GameEvent[] events, byte t)
    {
        turnNumber = t;
        uiController.LightUpPanel(true, false);
        NextEvent(events, new InfoThisPriority(), 0);
    }

    private class InfoThisPriority {
        internal GameEvent[] events = new GameEvent[0];
        internal int userBattery;
        internal int opponentBattery;
        internal int animationsToPlay = 0;
    }

    private void NextEvent(GameEvent[] events, InfoThisPriority infoThisPriority, int i)
    {
        if (i >= events.Length)
        {
            SetupNextTurn();
            return;
        }
        GameEvent e = events[i];
        if (infoThisPriority.events.Length == 0)
        {
            infoThisPriority.userBattery = boardController.GetMyBatteryScore();
            infoThisPriority.opponentBattery = boardController.GetOpponentBatteryScore();
        }
        UnityAction<int> Next = (int j) => NextEvent(events, infoThisPriority, j); 
        if (e is GameEvent.Resolve)
        {
            GameEvent.Resolve r = (GameEvent.Resolve)e;
            uiController.HighlightCommands(r.commandType, r.priority);
            int currentUserBattery = boardController.GetMyBatteryScore();
            int currentOpponentBattery = boardController.GetOpponentBatteryScore();
            boardController.SetBattery(infoThisPriority.userBattery, infoThisPriority.opponentBattery);
            history = Util.Add(history, SerializeState(turnNumber, r.priority, r.commandType));
            boardController.SetBattery(currentUserBattery, currentOpponentBattery);
            UnityAction callback = () =>
            {
                if (--infoThisPriority.animationsToPlay == 0) Next(i + 1);
            };
            foreach (GameEvent evt in infoThisPriority.events)
            {
                if (!evt.success) continue;
                RobotController primaryRobot = robotControllers.Get(evt.primaryRobotId);
                infoThisPriority.animationsToPlay++;
                if (evt is GameEvent.Move) primaryRobot.displayMove(((GameEvent.Move)evt).destinationPos, callback, boardController.PlaceRobot);
                else if (evt is GameEvent.Attack) primaryRobot.displayAttack(((GameEvent.Attack)evt).locs[0], callback);
                else if (evt is GameEvent.Death) primaryRobot.displayDeath(((GameEvent.Death)evt).returnHealth, callback, () => {
                    boardController.UnplaceRobot(primaryRobot);
                    DockController dock = !primaryRobot.isOpponent ? boardController.myDock : boardController.opponentDock;
                    primaryRobot.transform.parent = dock.transform;
                    primaryRobot.transform.localPosition = dock.PlaceInBelt();
                });
                else if (evt is GameEvent.Spawn) primaryRobot.displaySpawn(((GameEvent.Spawn)evt).destinationPos, callback, () => {
                    (primaryRobot.isOpponent ? boardController.opponentDock : boardController.myDock).RemoveFromBelt(primaryRobot.transform.localPosition);
                    primaryRobot.transform.parent = boardController.transform;
                    boardController.PlaceRobot(primaryRobot, ((GameEvent.Spawn)evt).destinationPos.x, ((GameEvent.Spawn)evt).destinationPos.y);
                });
                else infoThisPriority.animationsToPlay--;
                primaryRobot.clearEvents();
            }
            infoThisPriority.events = new GameEvent[0];
            if (infoThisPriority.animationsToPlay == 0) Next(i + 1);
        }
        else if (e is GameEvent.End)
        {
            GameEvent.End evt = (GameEvent.End)e;
            if (evt.primaryLost) boardController.GetMyBattery().transform.Rotate(Vector3.down * 90);
            if (evt.secondaryLost) boardController.GetOpponentBattery().transform.Rotate(Vector3.down * 90);
            uiController.Splash(evt.primaryLost);
            uiController.statsInterface.Initialize(evt);
            gameClient.SendEndGameRequest();
        }
        else
        {
            RobotController primaryRobot = robotControllers.Get(e.primaryRobotId);
            UnityAction callback = () => Next(i + 1);
            bool goNext = false;
            if (e is GameEvent.Move) primaryRobot.displayEvent(uiController.GetArrow("Move Arrow"), ((GameEvent.Move)e).destinationPos, callback);
            else if (e is GameEvent.Attack) Util.ForEach(((GameEvent.Attack)e).locs, (Vector2Int v) => primaryRobot.displayEvent(uiController.GetArrow("Attack Arrow"), v, callback));
            else if (e is GameEvent.Block) primaryRobot.displayEvent(uiController.GetArrow("Collision"), ((GameEvent.Block)e).deniedPos, callback);
            else if (e is GameEvent.Push) primaryRobot.displayEvent(uiController.GetArrow("Push"), new Vector2Int((int)primaryRobot.transform.position.x, (int)primaryRobot.transform.position.y) + ((GameEvent.Push)e).direction, callback);
            else if (e is GameEvent.Damage)
            {
                primaryRobot.displayEvent(uiController.GetArrow("Damage"), new Vector2Int((int)primaryRobot.transform.position.x, (int)primaryRobot.transform.position.y), callback);
                primaryRobot.displayHealth(((GameEvent.Damage)e).remainingHealth);
            }
            else if (e is GameEvent.Miss) Util.ForEach(((GameEvent.Miss)e).locs, (Vector2Int v) => primaryRobot.displayEvent(uiController.GetArrow("Missed Attack"), v, callback, false));
            else if (e is GameEvent.Battery)
            {
                Vector3 pos = (((GameEvent.Battery)e).isPrimary ? boardController.GetMyBattery(): boardController.GetOpponentBattery()).transform.position;
                primaryRobot.displayEvent(uiController.GetArrow("Damage"), new Vector2Int((int)pos.x, (int)pos.y), callback, false);
            }
            else if (e is GameEvent.Spawn) primaryRobot.displayEvent(null, new Vector2Int(((GameEvent.Spawn)e).destinationPos.x, ((GameEvent.Spawn)e).destinationPos.y), callback, false);
            else goNext = true;
            log.Info(e.ToString());
            boardController.SetBattery(e.primaryBattery, e.secondaryBattery);
            infoThisPriority.events = Util.Add(infoThisPriority.events, e);
            if (goNext) callback();
        }
    }

    private void SetupNextTurn()
    {
        robotControllers.ForEachValue(SetupRobotTurn);

        uiController.submitCommands.Deactivate();
        uiController.backToPresent.Deactivate();
        uiController.stepForwardButton.Deactivate();
        uiController.stepBackButton.SetActive(history.Length != 0);
        uiController.robotButtonContainer.SetButtons(true);
        uiController.LightUpPanel(false, false);

        currentHistoryIndex = history.Length;
        history = Util.Add(history, SerializeState((byte)(turnNumber + 1), GameConstants.MAX_PRIORITY, 0));
    }

    private void SetupRobotTurn(RobotController r)
    {
        r.gameObject.SetActive(true);
        uiController.ClearCommands(r.id);
        r.commands = new Command[0];
    }

    private HistoryState SerializeState(byte turn, byte priority, byte commandType)
    {
        HistoryState historyState = new HistoryState(turn, priority, commandType);
        historyState.SerializeRobots(robotControllers);
        historyState.SerializeTiles(boardController.GetAllTiles());
        historyState.SerializeScore(boardController.GetMyBatteryScore(), boardController.GetOpponentBatteryScore());
        return historyState;
    }

    private void DeserializeState(HistoryState historyState)
    {
        historyState.DeserializeRobots(robotControllers, uiController.AddSubmittedCommand);
        historyState.DeserializeTiles(boardController.GetAllTiles());
        historyState.DeserializeScore(boardController);
    }

    private void RefillCommands(RobotController r)
    {
        uiController.ClearCommands(r.id);
        Util.ForEach(r.commands, c => uiController.AddSubmittedCommand(c, r.id));
    }

    public void BackToPresent()
    {
        GoTo(history[history.Length - 1]);
    }

    public void StepForward()
    {
        GoTo(history[++currentHistoryIndex]);
    }

    public void StepBackward()
    {
        GoTo(history[--currentHistoryIndex]);
    }

    private void GoTo(HistoryState historyState)
    {
        DeserializeState(historyState);
        robotControllers.ForEachValue(RefillCommands);
        Util.ForEach(GameConstants.MAX_PRIORITY, p => ForEachPriorityHighlight(historyState, (byte)(p + 1))); 
        robotControllers.ForEachValue(uiController.ChangeToBoardLayer);

        bool isPresent = currentHistoryIndex == history.Length - 1;
        uiController.submitCommands.SetActive(isPresent && robotControllers.AnyValue(r => r.commands.Length > 0));
        uiController.stepForwardButton.SetActive(currentHistoryIndex < history.Length - 1);
        uiController.stepBackButton.SetActive(currentHistoryIndex > 0);
        uiController.backToPresent.SetActive(currentHistoryIndex < history.Length - 1);

        uiController.robotButtonContainer.SetButtons(isPresent);
        uiController.commandButtonContainer.SetButtons(false);
        uiController.directionButtonContainer.SetButtons(false);
    }
    
    private void ForEachPriorityHighlight(HistoryState state, byte p)
    {
        Util.ForEach(Command.NUM_TYPES, c => ForEachCommandHighlight(state, p, (byte)(c + 1)));
    }

    private void ForEachCommandHighlight(HistoryState state, byte p, byte c)
    {
        if (state.IsBeforeOrDuring(p, c)) uiController.HighlightCommands(c, p);
    }
}

