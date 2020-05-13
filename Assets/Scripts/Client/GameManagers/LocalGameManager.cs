using System.Collections.Generic;
using System.Linq;
using WarshopCommon;

public class LocalGameManager : BaseGameManager
{
    private bool myturn;
    private const string opponentName = "Opponent";

    internal LocalGameManager()
    {
        gameClient = new LocalGameClient();
        myturn = true;
    }

    protected override void InitializeSetupImpl(SetupController sc)
    {
        base.InitializeSetupImpl(sc);
        sc.opponentSquadPanel.gameObject.SetActive(true);
        sc.opponentSquadPanel.SetAddCallback(sc.AddSelectedToOpponentSquad);
        sc.backButton.onClick.AddListener(sc.EnterInitial);
        gameClient.AsLocal().ConnectToGameServer();
    }

    protected override void SendPlayerInfoImpl(string[] myRobotNames, string username)
    {
        myPlayer = new Game.Player(new Robot[0], username);
        opponentPlayer = new Game.Player(new Robot[0], opponentName);
        string[] opponentRobotNames = setupController.opponentSquadPanel.GetSquadRobotNames();
        gameClient.AsLocal().SendLocalGameRequest(myRobotNames, opponentRobotNames, myPlayer.name, opponentPlayer.name, LoadBoard);
    }

    protected override void SubmitCommands()
    {
        List<RobotController> robotsToSubmit = robotControllers.Values.ToList().FindAll(r => r.isOpponent == !myturn);
        Command[] commands = GetSubmittedCommands(robotsToSubmit);
        uiController.robotButtonContainer.EachMenuItem(m => m.gameObject.SetActive(!m.gameObject.activeInHierarchy));
        string username = myturn ? myPlayer.name : opponentPlayer.name;
        myturn = false;
        gameClient.SendSubmitCommands(commands, username, PlayEvents);
    }

    protected override void PlayEvents(GameEvent[] events)
    {
        uiController.actionButtonContainer.SetButtons(false);
        uiController.robotButtonContainer.SetButtons(false);
        uiController.commandButtonContainer.SetButtons(false);
        uiController.directionButtonContainer.SetButtons(false);
        base.PlayEvents(events);
        myturn = true;
    }
}
