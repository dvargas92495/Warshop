public class StandardGameManager : BaseGameManager
{
    internal new void SendPlayerInfoImpl(string[] myRobotNames, string username)
    {
        base.SendPlayerInfoImpl(myRobotNames, username);
        GameClient.SendGameRequest(myRobotNames, playerTurnObjectArray[0].name);
    }
}
