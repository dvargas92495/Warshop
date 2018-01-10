using System;
using UnityEngine;
using UnityEngine.Networking;

public class ClientController {

    private static Action onConnect;
    private static NetworkClient client = new NetworkClient();

    public static void Initialize () {
        client.RegisterHandler(MsgType.Connect, OnConnect);
        client.Connect("18.9.64.26", 12345);
    }

    private static void OnConnect(NetworkMessage netMsg)
    {
        Debug.Log("Client Connected");
        InterpreterController.SendPlayerInfo();
    }

    public static void SendBothTeams(String[] myRobots, String[] opponentRobots)
    {
        Debug.Log(string.Join(",", myRobots));
        Debug.Log(string.Join("|",opponentRobots));
        Messages.JoinGameMessage msg = new Messages.JoinGameMessage();
        msg.myRobots = myRobots;
        msg.opponentRobots = opponentRobots;
        client.Send(Messages.JOIN_GAME, msg);
    }

    public static void SubmitTurn (string message) {
    }
}
