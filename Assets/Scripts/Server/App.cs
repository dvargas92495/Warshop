using System;
using System.Collections;
using System.Collections.Generic;
using Z8.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class App : MonoBehaviour {

    // Use this for initialization
    public static void StartServer()
    {
        NetworkServer.RegisterHandler(MsgType.Connect, OnConnect);
        NetworkServer.RegisterHandler(Messages.START_LOCAL_GAME, OnStartLocalGame);
        NetworkServer.RegisterHandler(Messages.START_GAME, OnStartGame);
        NetworkServer.Listen(12345);
        Console.WriteLine("Listening");
    }

    private static void OnConnect(NetworkMessage netMsg)
    {
        Console.WriteLine("Client Connected");
    }

    private static void OnStartLocalGame(NetworkMessage netMsg)
    {
        Messages.StartLocalGameMessage msg = netMsg.ReadMessage<Messages.StartLocalGameMessage>();
        Messages.GameReadyMessage resp = new Messages.GameReadyMessage();
        resp.myname = "ME";
        resp.opponentname = "YOU";
        netMsg.conn.Send(Messages.GAME_READY, resp);
    }

    private static void OnStartGame(NetworkMessage netMsg)
    {
        Messages.StartLocalGameMessage msg = netMsg.ReadMessage<Messages.StartLocalGameMessage>();
        Messages.GameReadyMessage resp = new Messages.GameReadyMessage();
        resp.myname = "ME";
        resp.opponentname = "YOU";
        netMsg.conn.Send(Messages.GAME_READY, resp);
    }
}
