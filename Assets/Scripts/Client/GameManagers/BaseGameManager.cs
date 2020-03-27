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
    protected Dictionary<short, RobotController> robotControllers;

    protected byte turnNumber = 1;
    protected int currentHistoryIndex;
    protected List<HistoryState> history = new List<HistoryState>();
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
        if (instance == null) InitializeLocal();
        instance.InitializeSetupImpl(sc);
    }

    protected virtual void InitializeSetupImpl(SetupController sc)
    {
        setupController = sc;
    }

    public static void SendPlayerInfo(string[] myRobotNames, string username)
    {
        instance.SendPlayerInfoImpl(myRobotNames, username);
    }

    protected virtual void SendPlayerInfoImpl(string[] myRobotNames, string username)
    {
        myPlayer = new Game.Player(new Robot[0], username);
    }

    internal void LoadBoard(List<Robot> myTeam, List<Robot> opponentTeam, string opponentName, Map b)
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
        robotControllers = new Dictionary<short, RobotController>(myPlayer.team.GetLength() + opponentPlayer.team.GetLength());
        InitializePlayerRobots(myPlayer, boardController.myDock);
        InitializePlayerRobots(opponentPlayer, boardController.opponentDock);
    }

    private void InitializePlayerRobots(Game.Player player, DockController dock)
    {
        player.team.ForEach(r => InitializeRobot(r, dock));
    }

    private void InitializeRobot(Robot robot, DockController dock)
    {
        RobotController r = boardController.LoadRobot(robot, dock.transform);
        r.isOpponent = dock.Equals(boardController.opponentDock);
        robotControllers.Add(r.id, r);
        r.transform.localPosition = dock.PlaceInBelt();
        r.transform.localRotation = Quaternion.Euler(0, 0, r.isOpponent ? 180 : 0);
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
        history.Add(SerializeState(1, GameConstants.MAX_PRIORITY, 0));
    }

    protected abstract void SubmitCommands();

    protected Command[] GetSubmittedCommands(List<RobotController> robotsToSubmit)
    {
        uiController.LightUpPanel(true, true);
        List<Command> commands = robotsToSubmit.Reduce(new List<Command>(), AddCommands);
        uiController.commandButtonContainer.SetButtons(false);
        uiController.directionButtonContainer.SetButtons(false);
        uiController.submitCommands.Deactivate();
        return commands.ToArray();
    }

    private List<Command> AddCommands(List<Command> commands, RobotController robot)
    {
        robot.commands.ForEach(c => c.robotId = robot.id);
        List<Command> returnCommands = commands.Concat(robot.commands);
        uiController.ColorCommandsSubmitted(robot.id, robot.isOpponent);
        uiController.ChangeToBoardLayer(robot);
        return returnCommands;
    }

    protected virtual void PlayEvents(GameEvent[] events, byte t)
    {
        turnNumber = t;
        uiController.LightUpPanel(true, false);
        NextEvent(events, new InfoThisPriority(), 0);
    }

    private class InfoThisPriority {
        internal List<GameEvent> events = new List<GameEvent>();
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
        if (infoThisPriority.events.GetLength() == 0)
        {
            infoThisPriority.userBattery = boardController.GetMyBatteryScore();
            infoThisPriority.opponentBattery = boardController.GetOpponentBatteryScore();
        }
        UnityAction<int> Next = (int j) => NextEvent(events, infoThisPriority, j); 
        if (e is ResolveEvent)
        {
            ResolveEvent r = (ResolveEvent)e;
            uiController.HighlightCommands(r.commandType, r.priority);
            int currentUserBattery = boardController.GetMyBatteryScore();
            int currentOpponentBattery = boardController.GetOpponentBatteryScore();
            boardController.SetBattery(infoThisPriority.userBattery, infoThisPriority.opponentBattery);
            history.Add(SerializeState(turnNumber, r.priority, r.commandType));
            boardController.SetBattery(currentUserBattery, currentOpponentBattery);
            UnityAction callback = () =>
            {
                if (--infoThisPriority.animationsToPlay == 0) Next(i + 1);
            };
            infoThisPriority.events.ForEach(evt =>
            {
                if (!evt.success) return;
                infoThisPriority.animationsToPlay++;
                if (evt is MoveEvent) robotControllers.Get(((MoveEvent)evt).robotId).displayMove(((MoveEvent)evt).destinationPos, callback, boardController.PlaceRobot);
                else if (evt is AttackEvent) robotControllers.Get(((AttackEvent)evt).robotId).displayAttack(((AttackEvent)evt).locs.Get(0), callback);
                else if (evt is GameEvent.Death) robotControllers.Get(((GameEvent.Death)evt).robotId).displayDeath(((GameEvent.Death)evt).returnHealth, callback, () =>
                {
                    RobotController primaryRobot = robotControllers.Get(((GameEvent.Death)evt).robotId);
                    boardController.UnplaceRobot(primaryRobot);
                    DockController dock = !primaryRobot.isOpponent ? boardController.myDock : boardController.opponentDock;
                    primaryRobot.transform.parent = dock.transform;
                    primaryRobot.transform.localPosition = dock.PlaceInBelt();
                });
                else if (evt is SpawnEvent) robotControllers.Get(((SpawnEvent)evt).robotId).displaySpawn(((SpawnEvent)evt).destinationPos, callback, () =>
                {
                    RobotController primaryRobot = robotControllers.Get(((SpawnEvent)evt).robotId);
                    (primaryRobot.isOpponent ? boardController.opponentDock : boardController.myDock).RemoveFromBelt(primaryRobot.transform.localPosition);
                    primaryRobot.transform.parent = boardController.transform;
                    boardController.PlaceRobot(primaryRobot, ((SpawnEvent)evt).destinationPos.x, ((SpawnEvent)evt).destinationPos.y);
                });
                else infoThisPriority.animationsToPlay--;
            });
            infoThisPriority.events.Clear();
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
            UnityAction callback = () => Next(i + 1);
            bool goNext = false;
            if (e is MoveEvent) robotControllers.Get(((MoveEvent)e).robotId).displayEvent(uiController.GetArrow("Move Arrow"), ((MoveEvent)e).destinationPos, callback);
            else if (e is AttackEvent) ((AttackEvent)e).locs.ForEach(v => robotControllers.Get(((AttackEvent)e).robotId).displayEvent(uiController.GetArrow("Attack Arrow"), v, callback));
            else if (e is BlockEvent) robotControllers.Get(((BlockEvent)e).robotId).displayEvent(uiController.GetArrow("Collision"), ((BlockEvent)e).deniedPos, callback);
            else if (e is PushEvent) robotControllers.Get(((PushEvent)e).robotId).displayEvent(uiController.GetArrow("Push"), new Vector2Int((int)robotControllers.Get(((PushEvent)e).robotId).transform.position.x, (int)robotControllers.Get(((PushEvent)e).robotId).transform.position.y) + ((PushEvent)e).direction, callback);
            else if (e is DamageEvent)
            {
                RobotController primaryRobot = robotControllers.Get(((DamageEvent)e).robotId);
                primaryRobot.displayEvent(uiController.GetArrow("Damage"), new Vector2Int((int)primaryRobot.transform.position.x, (int)primaryRobot.transform.position.y), callback);
                primaryRobot.displayHealth(((DamageEvent)e).remainingHealth);
            }
            else if (e is MissEvent) ((MissEvent)e).locs.ForEach(v => robotControllers.Get(((MissEvent)e).robotId).displayEvent(uiController.GetArrow("Missed Attack"), v, callback, false));
            else if (e is GameEvent.Battery)
            {
                //Vector3 pos = (((GameEvent.Battery)e).isPrimary ? boardController.GetMyBattery(): boardController.GetOpponentBattery()).transform.position;
                //primaryRobot.displayEvent(uiController.GetArrow("Damage"), new Vector2Int((int)pos.x, (int)pos.y), callback, false);
            }
            else if (e is SpawnEvent) robotControllers.Get(((SpawnEvent)e).robotId).displayEvent(null, new Vector2Int(((SpawnEvent)e).destinationPos.x, ((SpawnEvent)e).destinationPos.y), callback, false);
            else goNext = true;
            log.Info(e.ToString());
            boardController.SetBattery(e.primaryBatteryCost, e.secondaryBatteryCost);
            infoThisPriority.events.Add(e);
            if (goNext) callback();
        }
    }

    private void SetupNextTurn()
    {
        robotControllers.ForEachValue(SetupRobotTurn);

        uiController.submitCommands.Deactivate();
        uiController.backToPresent.Deactivate();
        uiController.stepForwardButton.Deactivate();
        uiController.stepBackButton.SetActive(history.GetLength() != 0);
        uiController.robotButtonContainer.SetButtons(true);
        uiController.LightUpPanel(false, false);

        currentHistoryIndex = history.GetLength();
        history.Add(SerializeState((byte)(turnNumber + 1), GameConstants.MAX_PRIORITY, 0));
    }

    private void SetupRobotTurn(RobotController r)
    {
        r.gameObject.SetActive(true);
        uiController.ClearCommands(r.id, r.isOpponent);
        r.commands.Clear();
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
        uiController.ClearCommands(r.id, r.isOpponent);
        r.commands.ForEach(c => uiController.AddSubmittedCommand(c, r.id, r.isOpponent));
    }

    public void BackToPresent()
    {
        GoTo(history.Get(history.GetLength() - 1));
    }

    public void StepForward()
    {
        GoTo(history.Get(++currentHistoryIndex));
    }

    public void StepBackward()
    {
        GoTo(history.Get(--currentHistoryIndex));
    }

    private void GoTo(HistoryState historyState)
    {
        DeserializeState(historyState);
        robotControllers.ForEachValue(RefillCommands);
        Util.ToIntList(GameConstants.MAX_PRIORITY).ForEach(p => ForEachPriorityHighlight(historyState, (byte)(p + 1)));
        robotControllers.ForEachValue(uiController.ChangeToBoardLayer);

        bool isPresent = currentHistoryIndex == history.GetLength() - 1;
        uiController.submitCommands.SetActive(isPresent && robotControllers.AnyValue(r => r.commands.GetLength() > 0));
        uiController.stepForwardButton.SetActive(currentHistoryIndex < history.GetLength() - 1);
        uiController.stepBackButton.SetActive(currentHistoryIndex > 0);
        uiController.backToPresent.SetActive(currentHistoryIndex < history.GetLength() - 1);

        uiController.robotButtonContainer.SetButtons(isPresent);
        uiController.commandButtonContainer.SetButtons(false);
        uiController.directionButtonContainer.SetButtons(false);
    }
    
    private void ForEachPriorityHighlight(HistoryState state, byte p)
    {
        new List<byte>(Command.TYPES).ForEach(c => ForEachCommandHighlight(state, p, (byte)(c + 1)));
    }

    private void ForEachCommandHighlight(HistoryState state, byte p, byte c)
    {
        if (state.IsBeforeOrDuring(p, c)) uiController.HighlightCommands(c, p);
    }
}

