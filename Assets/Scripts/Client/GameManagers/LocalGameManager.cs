public class LocalGameManager : BaseGameManager
{
    internal LocalGameManager()
    {
        gameClient = new LocalGameClient();
    }

    internal new void InitializeSetupImpl(SetupController sc)
    {
        base.InitializeSetupImpl(sc);
        sc.opponentSquadPanel.gameObject.SetActive(true);
        sc.opponentSquadPanel.SetAddCallback(sc.AddSelectedToOpponentSquad);
        gameClient.AsLocal().ConnectToGameServer(LoadBoard);
        
        if (sc.playtest != null)
        {
            string[] lines = sc.playtest.text.Split('\n');
            string[] mybots = lines[1].Trim().Split(',');
            string[] opbots = lines[2].Trim().Split(',');
            string myname = lines[3];
            string op = lines[4];
            setupController.statusModal.ShowLoading();
            //SendPlayerInfo(myname, op, mybots, opbots);
        }
    }

    internal new void SendPlayerInfoImpl(string[] myRobotNames, string username)
    {
        gameOver = false;
        myPlayer = new Game.Player(new Robot[0], username);
        opponentPlayer = new Game.Player(new Robot[0], setupController.opponentName.text);
        string[] opponentRobotNames = setupController.opponentSquadPanel.GetSquadRobotNames();
        gameClient.AsLocal().SendLocalGameRequest(myRobotNames, opponentRobotNames, myPlayer.name, opponentPlayer.name);
    }
}
