using System;
using UnityEngine;
using UnityEngine.Networking;
using Z8.Generic;

public class GameClient {

    private static Action onConnect;
    private static NetworkClient client = new NetworkClient();

    public static void Initialize () {
        client.RegisterHandler(MsgType.Connect, OnConnect);
        client.RegisterHandler(Messages.GAME_READY, OnGameReady);
        client.Connect("18.9.64.27", 12345);
    }

    private static void OnConnect(NetworkMessage netMsg)
    {
        Debug.Log("Client Connected");
        Interpreter.SendPlayerInfo();
    }

    private static void OnGameReady(NetworkMessage netMsg)
    {
        Messages.GameReadyMessage msg = netMsg.ReadMessage<Messages.GameReadyMessage>();
        PlayerTurnObject[] playerTurnObjects = new PlayerTurnObject[2];
        playerTurnObjects[0] = new PlayerTurnObject(msg.myname);
        playerTurnObjects[1] = new PlayerTurnObject(msg.opponentname);
        Interpreter.LoadBoard(playerTurnObjects);
    }

    public static void SendLocalGameRequest(String[] myRobots, String[] opponentRobots)
    {
        Messages.StartLocalGameMessage msg = new Messages.StartLocalGameMessage();
        msg.myRobots = myRobots;
        msg.opponentRobots = opponentRobots;
        client.Send(Messages.START_LOCAL_GAME, msg);
    }

    public static void SendGameRequest(String[] myRobots)
    {
        Messages.StartLocalGameMessage msg = new Messages.StartLocalGameMessage();
        msg.myRobots = myRobots;
        client.Send(Messages.START_LOCAL_GAME, msg);
    }

    public static void SubmitTurn (string message) {
    }
}
