using System;
using Z8.Generic;
using UnityEngine.Networking;

public class Messages {
    public static short START_LOCAL_GAME = MsgType.Highest + 1;
    public static short START_GAME = MsgType.Highest + 2;
    public static short GAME_READY = MsgType.Highest + 3;
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
        //TODO Robot things
    }
}
