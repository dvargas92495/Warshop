public class StandardGameManager : BaseGameManager
{
    internal StandardGameManager(string playerSessionId, string ipAddress, int port)
    {
        gameClient = new AwsGameClient(playerSessionId, ipAddress, port);
    }

    protected override void InitializeSetupImpl(SetupController sc)
    {
        base.InitializeSetupImpl(sc);
        sc.backButton.onClick.AddListener(sc.EnterLobby);
        gameClient.AsAws().ConnectToGameServer(setupController.statusModal.DisplayError);
    }

    protected override void SendPlayerInfoImpl(string[] myRobotNames, string username)
    {
        base.SendPlayerInfoImpl(myRobotNames, username);
        gameClient.AsAws().SendGameRequest(myRobotNames, myPlayer.name, LoadBoard);
    }

    protected override void SubmitCommands()
    {
        Command[] commands = GetSubmittedCommands(robotControllers.ToValueList());
        uiController.actionButtonContainer.SetButtons(false);
        uiController.robotButtonContainer.SetButtons(false);
        gameClient.SendSubmitCommands(commands, myPlayer.name);
    }
}
