public class StandardGameManager : BaseGameManager
{
    internal StandardGameManager(string playerSessionId, string ipAddress, int port)
    {
        gameClient = new AwsGameClient(playerSessionId, ipAddress, port);
    }

    internal new void InitializeSetupImpl(SetupController sc)
    {
        base.InitializeSetupImpl(sc);
        gameClient.AsAws().ConnectToGameServer(LoadBoard, setupController.statusModal.DisplayError);
    }

    internal new void SendPlayerInfoImpl(string[] myRobotNames, string username)
    {
        base.SendPlayerInfoImpl(myRobotNames, username);
        gameClient.AsAws().SendGameRequest(myRobotNames, myPlayer.name);
    }
}
