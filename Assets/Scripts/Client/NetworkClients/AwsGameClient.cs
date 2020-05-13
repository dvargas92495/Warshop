using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using WarshopCommon;

public class AwsGameClient : GameClient
{
    private NetworkClient client;
    private string ip;
    private int port;
    private string playerSessionId;

    internal AwsGameClient(string psid, string ipAddress, int p)
    {
        playerSessionId = psid;
        ip = ipAddress;
        port = p;
    }

    internal void ConnectToGameServer(UnityAction<string> errorCallback)
    {
        try
        {
            client = new NetworkClient();
            short[] messageTypes = new short[] {
                MsgType.Connect, MsgType.Disconnect, MsgType.Error, Messages.GAME_READY, Messages.TURN_EVENTS, Messages.WAITING_COMMANDS, Messages.SERVER_ERROR,
            };
            messageTypes.ToList().ForEach(messageType => client.RegisterHandler(messageType, GetHandler(messageType)));
            client.RegisterHandler(MsgType.Connect, OnConnect);
            client.RegisterHandler(MsgType.Disconnect, OnDisconnect);
            client.Connect(ip, port);
            Debug.Log("Attempting to connect to " + ip + ":" + port);
        }
        catch(Exception e)
        {
            Debug.LogError(e);
            errorCallback("An unexpected error occurred! Please notify the developers.");
        }
    }

    protected void Send(short msgType, MessageBase message)
    {
        client.Send(msgType, message);
    }

    protected new NetworkMessageDelegate GetHandler(short messageType)
    {
        switch(messageType)
        {
            case MsgType.Connect:
                return OnConnect;
            case MsgType.Disconnect:
                return OnDisconnect;
            default:
                return default(NetworkMessageDelegate);
        }
    }

    private void OnConnect(NetworkMessage netMsg)
    {
        Debug.Log("Connected");

        Messages.AcceptPlayerSessionMessage msg = new Messages.AcceptPlayerSessionMessage();
        msg.playerSessionId = playerSessionId;
   //     Send(Messages.ACCEPT_PLAYER_SESSION, msg);
    }

    private void OnDisconnect(NetworkMessage netMsg)
    {
        Debug.Log("Disconnected");
        client.Connect(ip, port);
    }

    internal void SendGameRequest(string[] myRobots, string myname, UnityAction<List<Robot>, List<Robot>, string, Map> readyCallback)
    {
        Messages.StartGameMessage msg = new Messages.StartGameMessage();
        msg.myName = myname;
        msg.myRobots = myRobots;
        gameReadyCallback = readyCallback;
    //    Send(Messages.START_GAME, msg);
    }
}
