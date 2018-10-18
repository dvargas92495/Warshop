public class LocalGameManager : BaseGameManager
{
    internal new void InitializeSetup(SetupController sc)
    {
        base.InitializeSetup(sc);
        if (sc.playtest != null)
        {
            string[] lines = sc.playtest.text.Split('\n');
            string[] mybots = lines[1].Trim().Split(',');
            string[] opbots = lines[2].Trim().Split(',');
            string myname = lines[3];
            string op = lines[4];
            setupController.ShowLoading();
            SendPlayerInfo(myname, op, mybots, opbots);
        }
    }
}
