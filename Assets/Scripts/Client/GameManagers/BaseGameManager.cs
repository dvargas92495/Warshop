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

    protected virtual void PlayEvent(GameEvent[] events, int index) 
    {
        if (index == events.Length) {
            SetupNextTurn();
            return;
        }
        UnityAction Next = () => {
            Debug.Log("abt to call " + (index+1));
            PlayEvent(events, index+1);
        };
        GameEvent e = events[index];
        Debug.Log(e);
        boardController.DiffBattery(e.primaryBatteryCost, e.secondaryBatteryCost);
        if (e is ResolveEvent) {
            ResolveEvent re = (ResolveEvent)e;
            uiController.HighlightCommands(re.priority);
            Counter animationsToPlay = new Counter(re.GetNumResolutions());
            UnityAction callback = () => {
                animationsToPlay.Decrement();
                if (animationsToPlay.Get() <= 0) Next();
            };
            re.robotIdToSpawn.ForEach(p => {
                RobotController primaryRobot = robotControllers.Get(p.GetLeft());
                (primaryRobot.isOpponent ? boardController.opponentDock : boardController.myDock).RemoveFromBelt(primaryRobot.transform.localPosition);
                primaryRobot.transform.parent = boardController.transform;
                boardController.PlaceRobot(primaryRobot, p.GetRight().x, p.GetRight().y);
                primaryRobot.displaySpawn(p.GetRight(), callback);
            });
            re.robotIdToMove.ForEach(p => {
                RobotController primaryRobot = robotControllers.Get(p.GetLeft());
                primaryRobot.displayMove(p.GetRight(), boardController, callback);
            });
            re.robotIdToHealth.ForEach(t => {
                RobotController primaryRobot = robotControllers.Get(t.GetLeft());
                primaryRobot.displayDamage(t.GetRight(), callback);
            });
            if (re.myBatteryHit) boardController.GetMyBattery().DisplayDamage(callback);
            if (re.opponentBatteryHit) boardController.GetOpponentBattery().DisplayDamage(callback);
            re.missedAttacks.ForEach(v => boardController.DisplayMiss(v, callback));
            re.robotIdsBlocked.ForEach(r => robotControllers.Get(r).DisplayBlocked(callback));
        }
        else if (e is SpawnEvent) robotControllers.Get(((SpawnEvent)e).robotId).displaySpawnRequest(Next);
        else if (e is MoveEvent) robotControllers.Get(((MoveEvent)e).robotId).displayMoveRequest(((MoveEvent)e).destinationPos, Next);
        else if (e is AttackEvent) ((AttackEvent)e).locs.ForEach(v => {
            Debug.Log("attackin: "+v);
            robotControllers.Get(((AttackEvent)e).robotId).displayAttack(v, Next);
        });
        else if (e is EndEvent)
        {
            EndEvent evt = (EndEvent)e;
            Counter animationsToPlay = new Counter((evt.primaryLost ? 1 : 0) + (evt.secondaryLost ? 1 : 0));
            UnityAction callback = () => {
                animationsToPlay.Decrement();
                if (animationsToPlay.Get() <= 0) {
                    uiController.robotButtonContainer.SetButtons(false);
                    uiController.statsInterface.Initialize(evt);
                    gameClient.SendEndGameRequest();
                }
            };
            if (evt.primaryLost) boardController.GetMyBattery().DisplayEnd(callback);
            if (evt.secondaryLost) boardController.GetOpponentBattery().DisplayEnd(callback);
        }
        else PlayEvent(events, index + 1);
    }

    protected virtual void PlayEvents(GameEvent[] events)
    {
        uiController.LightUpPanel(true, false);
        Debug.Log(new List<GameEvent>(events));
        PlayEvent(events, 0);
        /*
            if (e is ResolveEvent)
            {
                history.Add(SerializeState(turnNumber, r.priority, r.commandType));
                eventsThisPriority.ForEach(evt =>
                {
                    if (evt is GameEvent.Death) robotControllers.Get(((GameEvent.Death)evt).robotId).displayDeath(((GameEvent.Death)evt).returnHealth, (RobotController primaryRobot) =>
                    {
                        boardController.UnplaceRobot(primaryRobot);
                        DockController dock = !primaryRobot.isOpponent ? boardController.myDock : boardController.opponentDock;
                        primaryRobot.transform.parent = dock.transform;
                        primaryRobot.transform.localPosition = dock.PlaceInBelt();
                    });
                });
            }
        }
        */
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
        turnNumber += 1;
        history.Add(SerializeState((byte)(turnNumber), GameConstants.MAX_PRIORITY, 0));
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
        new List<byte>(Command.TYPES).ForEach(c => ForEachCommandHighlight(state, p));
    }

    private void ForEachCommandHighlight(HistoryState state, byte p)
    {
        if (state.IsBeforeOrDuring(p)) uiController.HighlightCommands(p);
    }
}

