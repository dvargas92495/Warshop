using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Aws.GameLift;
using Aws.GameLift.Server;

public class AwsApp : App
{
    private const int MIN_PORT = 12350;
    private const int MAX_PORT = 12360;
    private static readonly Logger log = new Logger(typeof(App));

    public AwsApp()
    {
        Logger.Setup(true);
        GenericOutcome outcome = GameLiftServerAPI.InitSDK();
        Application.targetFrameRate = 60;
        if (outcome.Success)
        {
            short[] messageTypes = {
                MsgType.Connect, MsgType.Disconnect, Messages.ACCEPT_PLAYER_SESSION, Messages.START_LOCAL_GAME,
                Messages.START_GAME, Messages.SUBMIT_COMMANDS, Messages.END_GAME,
            };
            Util.ForEach(messageTypes, messageType => NetworkServer.RegisterHandler(messageType, GetHandler(messageType)));
            int port = MIN_PORT;
            for (; port < MAX_PORT; port++)
            {
                if (NetworkServer.Listen(port)) break;
            }
            GameLiftServerAPI.ProcessReady(new ProcessParameters(
                OnGameSession,
                OnProcessTerminate,
                OnHealthCheck,
                port,
                new LogParameters(new List<string>()
                {
                    GameConstants.APP_LOG_DIR
                })
            ));
            log.Info("Listening on: " + port);
        }
        else
        {
            log.Error(outcome);
        }
    }

    protected override void Send(int connId, short msgType, MessageBase msg)
    {
        NetworkServer.connections[connId].Send(msgType, msg);
    }

    protected new NetworkMessageDelegate GetHandler(short messageType)
    {
        switch (messageType)
        {
            case Messages.ACCEPT_PLAYER_SESSION:
                return OnAcceptPlayerSession;
            default:
                return base.GetHandler(messageType);
        }
    }

    private void OnAcceptPlayerSession(NetworkMessage netMsg)
    {
        Messages.AcceptPlayerSessionMessage msg = netMsg.ReadMessage<Messages.AcceptPlayerSessionMessage>();
        GenericOutcome outcome = GameLiftServerAPI.AcceptPlayerSession(msg.playerSessionId);
        if (!outcome.Success)
        {
            log.Error(outcome);
            return;
        }
    }
}
