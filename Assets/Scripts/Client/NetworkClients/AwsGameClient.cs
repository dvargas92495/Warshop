using System;
using UnityEngine.Events;
using UnityEngine.Networking;

public class AwsGameClient : GameClient
{
    private NetworkClient client;
    private string ip;
    private int port;
    private string playerSessionId;
    private Logger log = new Logger(typeof(AwsGameClient));

    internal AwsGameClient(string psid, string ipAddress, int p)
    {
        playerSessionId = psid;
        ip = ipAddress;
        port = p;
    }

    internal void ConnectToGameServer(UnityAction<Robot[], Robot[], string, Map> readyCallback, UnityAction<string> errorCallback)
    {
        try
        {
            gameReadyCallback = readyCallback;
            client = new NetworkClient();
            short[] messageTypes = new short[] {
                MsgType.Connect, MsgType.Disconnect, MsgType.Error, Messages.GAME_READY, Messages.TURN_EVENTS, Messages.WAITING_COMMANDS, Messages.SERVER_ERROR,
            };
            Util.ForEach(messageTypes, messageType => client.RegisterHandler(messageType, GetHandler(messageType)));
            client.RegisterHandler(MsgType.Connect, OnConnect);
            client.RegisterHandler(MsgType.Disconnect, OnDisconnect);
            client.Connect(ip, port);
            log.Info("Attempting to connect to " + ip + ":" + port);
        }
        catch (Exception e)
        {
            log.Fatal(e);
            errorCallback("An unexpected error occurred! Please notify the developers.");
        }
    }

    protected override void Send(short msgType, MessageBase message)
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
                return base.GetHandler(messageType);
        }
    }

    private void OnConnect(NetworkMessage netMsg)
    {
        log.Info("Connected");
        BaseGameManager.ClientError("");

        Messages.AcceptPlayerSessionMessage msg = new Messages.AcceptPlayerSessionMessage();
        msg.playerSessionId = playerSessionId;
        Send(Messages.ACCEPT_PLAYER_SESSION, msg);
    }

    private void OnDisconnect(NetworkMessage netMsg)
    {
        log.Info("Disconnected");
        BaseGameManager.ClientError("Disconnected, attempting to reconnect");
        client.Connect(ip, port);
    }

    internal void SendGameRequest(string[] myRobots, string myname)
    {
        Messages.StartGameMessage msg = new Messages.StartGameMessage();
        msg.myName = myname;
        msg.myRobots = myRobots;
        Send(Messages.START_GAME, msg);
    }
}
