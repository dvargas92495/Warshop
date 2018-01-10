using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class App : MonoBehaviour {

    // Use this for initialization
    public static void StartServer()
    {
        NetworkServer.RegisterHandler(MsgType.Connect, OnConnect);
        NetworkServer.RegisterHandler(Messages.JOIN_GAME, OnJoinGame);
        NetworkServer.Listen(12345);
        Console.WriteLine("Listening");
    }

    private static void OnConnect(NetworkMessage netMsg)
    {
        Console.WriteLine("Client Connected");
    }

    private static void OnJoinGame(NetworkMessage netMsg)
    {
        Messages.JoinGameMessage msg = netMsg.ReadMessage<Messages.JoinGameMessage>();

        Console.WriteLine(string.Join(",", msg.myRobots));
        Console.WriteLine(string.Join("|", msg.opponentRobots));
    }
}
