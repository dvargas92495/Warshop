using UnityEngine.Events;
using UnityEngine.Networking;

public abstract class GameClient
{
    protected UnityAction<List<Robot>, List<Robot>, string, Map> gameReadyCallback;
    internal static string username;
    private static Logger log = new Logger(typeof(GameClient).ToString());

    protected abstract void Send(short msgType, MessageBase message);

    protected NetworkMessageDelegate GetHandler(short messageType)
    {
        switch(messageType)
        {
            case MsgType.Error:
                return OnNetworkError;
            case Messages.GAME_READY:
                return OnGameReady;
            case Messages.TURN_EVENTS:
                return OnTurnEvents;
            case Messages.WAITING_COMMANDS:
                return OnOpponentWaiting;
            case Messages.SERVER_ERROR:
                return OnServerError;
            default:
                return OnUnsupportedMessage;
        }
    }

    protected void OnUnsupportedMessage(NetworkMessage netMsg)
    {
        log.Info("Unsupported message type: " + netMsg.msgType);
    }

    protected static void OnNetworkError(NetworkMessage netMsg)
    {
        log.Info("Network Error");
    }

    internal void OnGameReady(NetworkMessage netMsg)
    {
        Messages.GameReadyMessage msg = netMsg.ReadMessage<Messages.GameReadyMessage>();
        log.Info("Received Game Information");
        gameReadyCallback(Util.ToList(msg.myTeam), Util.ToList(msg.opponentTeam), msg.opponentname, msg.board);
    }

    protected void OnTurnEvents(NetworkMessage netMsg)
    {
        Messages.TurnEventsMessage msg = netMsg.ReadMessage<Messages.TurnEventsMessage>();
        //BaseGameManager.PlayEvents(msg.events, msg.turn);
    }

    protected void OnOpponentWaiting(NetworkMessage netMsg)
    {
        //BaseGameManager.uiController.LightUpPanel(!GameConstants.LOCAL_MODE, false);
    }

    protected void OnServerError(NetworkMessage netMsg)
    {
        Messages.ServerErrorMessage msg = netMsg.ReadMessage<Messages.ServerErrorMessage>();
        log.Fatal(msg.serverMessage + ": " + msg.exceptionType + " - " + msg.exceptionMessage);
    }
    
    internal void SendSubmitCommands (Command[] commands, string owner) {
        Messages.SubmitCommandsMessage msg = new Messages.SubmitCommandsMessage();
        msg.commands = commands;
        msg.owner = owner;
        Send(Messages.SUBMIT_COMMANDS, msg);
    }

    internal void SendEndGameRequest ()
    {
        Send(Messages.END_GAME, new Messages.EndGameMessage());
    }

    internal LocalGameClient AsLocal()
    {
        return (LocalGameClient)this;
    }

    internal AwsGameClient AsAws()
    {
        return (AwsGameClient)this;
    }
}
