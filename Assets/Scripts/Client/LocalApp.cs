using UnityEngine.Networking;
using Aws.GameLift.Server.Model;

public class LocalApp : App
{
    LocalGameClient gameClient;

    private static readonly Logger log = new Logger(typeof(LocalApp));

    protected override void Send(int connId, short msgType, MessageBase msg)
    {
        gameClient.Receive(msgType, msg);
    }

    protected new void OnConnect(NetworkMessage netMsg)
    {
        log.Info(netMsg, "Client Connected");
        OnGameSession(new GameSession());
        gameClient.Receive(MsgType.Connect, Messages.EMPTY);
    }
}
