using UnityEngine;
using UnityEngine.Networking;
using Aws.GameLift;
using Aws.GameLift.Server;

public class AwsApp : App
{
    private const int PORT = 12345;
    private static readonly Logger log = new Logger(typeof(AwsApp).ToString());

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
            Util.ToList(messageTypes).ForEach(messageType => NetworkServer.RegisterHandler(messageType, GetHandler(messageType)));
            NetworkServer.Listen(PORT);
            LogParameters paths = new LogParameters();
            paths.LogPaths.Add(GameConstants.APP_LOG_DIR);
            GameLiftServerAPI.ProcessReady(new ProcessParameters(
                OnGameSession,
                OnProcessTerminate,
                OnHealthCheck,
                PORT,
                paths
            ));
            log.Info("Listening on: " + PORT);
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
