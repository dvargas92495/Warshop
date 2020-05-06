using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public abstract class GameClient
{
    protected UnityAction<List<Robot>, List<Robot>, string, Map> gameReadyCallback;
    protected UnityAction<GameEvent[]> onTurnCallback;

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
        Debug.Log("Unsupported message type: " + netMsg.msgType);
    }

    protected static void OnNetworkError(NetworkMessage netMsg)
    {
        Debug.Log("Network Error");
    }

    internal void OnGameReady(NetworkMessage netMsg)
    {
        Messages.GameReadyMessage msg = netMsg.ReadMessage<Messages.GameReadyMessage>();
        Debug.Log("Received Game Information");
        gameReadyCallback(Util.ToList(msg.myTeam), Util.ToList(msg.opponentTeam), msg.opponentname, msg.board);
    }

    protected void OnTurnEvents(NetworkMessage netMsg)
    {
        Debug.Log("Received Turn Events");
        Messages.TurnEventsMessage msg = netMsg.ReadMessage<Messages.TurnEventsMessage>();
        onTurnCallback(msg.events);
    }

    protected void OnOpponentWaiting(NetworkMessage netMsg)
    {
        //BaseGameManager.uiController.LightUpPanel(!GameConstants.LOCAL_MODE, false);
    }

    protected void OnServerError(NetworkMessage netMsg)
    {
        Messages.ServerErrorMessage msg = netMsg.ReadMessage<Messages.ServerErrorMessage>();
        Debug.LogError(msg.serverMessage + ": " + msg.exceptionType + " - " + msg.exceptionMessage);
    }
    
    internal void SendSubmitCommands (Command[] commands, string owner, UnityAction<GameEvent[]> callback) {
        Messages.SubmitCommandsMessage msg = new Messages.SubmitCommandsMessage();
        msg.commands = commands;
        msg.owner = owner;
        onTurnCallback = callback;
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
