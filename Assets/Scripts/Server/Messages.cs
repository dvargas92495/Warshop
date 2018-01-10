using System;
using UnityEngine.Networking;

public class Messages {
    public static short JOIN_GAME = MsgType.Highest + 1;
    public class JoinGameMessage : MessageBase
    {
        public String[] myRobots;
        public String[] opponentRobots;
    }
}
