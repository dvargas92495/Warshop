using System;
using UnityEngine;
using UnityEngine.Networking;

public class Messages {
    public const short START_LOCAL_GAME = MsgType.Highest + 1;
    public const short START_GAME = MsgType.Highest + 2;
    public const short GAME_READY = MsgType.Highest + 3;
    public const short SUBMIT_COMMANDS = MsgType.Highest + 4;
    public const short TURN_EVENTS = MsgType.Highest + 5;
    public class EmptyMessage : MessageBase { }
    public static EmptyMessage EMPTY = new EmptyMessage();
    public class StartLocalGameMessage : MessageBase
    {
        public String[] myRobots;
        public String[] opponentRobots;
    }
    public class StartGameMessage : MessageBase
    {
        public String[] myRobots;
    }
    public class GameReadyMessage : MessageBase
    {
        public String myname;
        public String opponentname;
        public byte numRobots;
        public string[] robotNames;
        public byte[] robotHealth;
        public byte[] robotAttacks;
        public byte[] robotPriorities;
        public bool[] robotIsOpponents;
    }
    public class SubmitCommandsMessage : MessageBase
    {
        public Command[] commands;
        public string owner;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(commands.Length);
            Array.ForEach(commands, (Command cmd) => cmd.Serialize(writer));
            writer.Write(owner);
        }
        public override void Deserialize(NetworkReader reader)
        {
            commands = new Command[reader.ReadInt32()];
            for (int i = 0; i < commands.Length; i++)
            {
                commands[i] = Command.Deserialize(reader);
            }
            owner = reader.ReadString();
        }
    }
    public class TurnEventsMessage : MessageBase
    {
        public GameEvent[] events;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(events.Length);
            Array.ForEach(events, (GameEvent evt) => evt.Serialize(writer));
        }
        public override void Deserialize(NetworkReader reader)
        {
            events = new GameEvent[reader.ReadInt32()];
            for (int i = 0; i < events.Length; i++)
            {
                events[i] = GameEvent.Deserialize(reader);
            }
        }
    }
}
