using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Networking;
using WarshopCommon;

public class LocalGameClient : GameClient
{

    internal void ConnectToGameServer()
    {
    }
    internal void SendLocalGameRequest(string[] myRobots, string[] opponentRobots, string myname, string opponentname, UnityAction<List<Robot>, List<Robot>, string, Map> readyCallback)
    {
        Messages.StartLocalGameMessage msg = new Messages.StartLocalGameMessage();
        msg.myRobots = myRobots;
        msg.opponentRobots = opponentRobots;
        msg.myName = myname;
        msg.opponentName = opponentname;
        gameReadyCallback = readyCallback;
      //  Send(Messages.START_LOCAL_GAME, msg);
    }
}
