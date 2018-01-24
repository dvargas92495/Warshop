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
    public class FakeConnectMessage : MessageBase
    {
        public string boardFile;
    }
    public class StartLocalGameMessage : MessageBase
    {
        public String playerSessionId;
        public String myName;
        public String opponentName;
        public String[] myRobots;
        public String[] opponentRobots;
    }
    public class StartGameMessage : MessageBase
    {
        public String playerSessionId;
        public String myName;
        public String[] myRobots;
    }
    public class GameReadyMessage : MessageBase
    {
        public String opponentname;
        public Robot[] myTeam;
        public Robot[] opponentTeam;
        public Map board;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(opponentname);
            writer.Write(myTeam.Length);
            Array.ForEach(myTeam, (Robot robot) => robot.Serialize(writer));
            writer.Write(opponentTeam.Length);
            Array.ForEach(opponentTeam, (Robot robot) => robot.Serialize(writer));
            board.Serialize(writer);
        }
        public override void Deserialize(NetworkReader reader)
        {
            opponentname = reader.ReadString();
            myTeam = new Robot[reader.ReadInt32()];
            for (int i = 0; i < myTeam.Length; i++)
            {
                myTeam[i] = Robot.Deserialize(reader);
            }
            opponentTeam = new Robot[reader.ReadInt32()];
            for (int i = 0; i < opponentTeam.Length; i++)
            {
                opponentTeam[i] = Robot.Deserialize(reader);
            }
            board = Map.Deserialize(reader);
        }
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
            Array.ForEach(events, (GameEvent evt) =>
            {
                evt.Serialize(writer);
                evt.FinishMessage(writer);
            });
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
