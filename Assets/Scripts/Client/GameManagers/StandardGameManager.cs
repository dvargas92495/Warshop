public class StandardGameManager : BaseGameManager
{
    internal StandardGameManager(string playerSessionId, string ipAddress, int port)
    {
        gameClient = new AwsGameClient(playerSessionId, ipAddress, port);
    }

    protected new void InitializeSetupImpl(SetupController sc)
    {
        base.InitializeSetupImpl(sc);
        gameClient.AsAws().ConnectToGameServer(LoadBoard, setupController.statusModal.DisplayError);
    }

    protected new void SendPlayerInfoImpl(string[] myRobotNames, string username)
    {
        base.SendPlayerInfoImpl(myRobotNames, username);
        gameClient.AsAws().SendGameRequest(myRobotNames, myPlayer.name);
    }

    protected override void SubmitCommands()
    {
        Command[] commands = GetSubmittedCommands();
        uiController.actionButtonContainer.SetButtons(false);
        uiController.robotButtonContainer.SetButtons(false);
        gameClient.SendSubmitCommands(commands, myPlayer.name);
    }
}
