namespace WarshopCommon {
    public class Messages {
        public const short START_LOCAL_GAME = 1;
        public const short START_GAME = 2;
        public const short GAME_READY = 3;
        public const short SUBMIT_COMMANDS = 4;
        public const short TURN_EVENTS = 5;
        public const short WAITING_COMMANDS = 6;
        public const short SERVER_ERROR = 7;
        public const short END_GAME = 8;
        public const short ACCEPT_PLAYER_SESSION = 9;
        public class AcceptPlayerSessionMessage
        {
            public string playerSessionId {get; set;}
        }
        public class StartLocalGameMessage
        {
            public string myName {get; set;}
            public string opponentName {get; set;}
            public string[] myRobots {get; set;}
            public string[] opponentRobots {get; set;}
        }
        public class StartGameMessage
        {
            public string myName {get; set;}
            public string[] myRobots {get; set;}
        }
        public class GameReadyMessage
        {
            public bool isPrimary {get; set;}
            public string opponentname {get; set;}
            public Robot[] myTeam {get; set;}
            public Robot[] opponentTeam {get; set;}
            public Map board {get; set;}
        }
        public class SubmitCommandsMessage
        {
            public Command[] commands {get; set;}
            public string owner {get; set;}
        }
        public class TurnEventsMessage
        {
            public GameEvent[] events {get; set;}
            public byte turn {get; set;}
        }
        public class OpponentWaitingMessage { }
        public class ServerErrorMessage
        {
            public string serverMessage {get; set;}
            public string exceptionType {get; set;}
            public string exceptionMessage {get; set;}
        }
        public class EndGameMessage { }

        //Gateway Objects, TODO: Get rid of repeated classes
        public class CreateGameRequest
        {
            public string playerId {get; set;}
            public bool isPrivate {get; set;}
            public string password {get; set;}
        }

        public class JoinGameRequest
        {
            public string playerId {get; set;}
            public string gameSessionId {get; set;}
            public string password {get; set;}
        }
        public class GetGamesResponse
        {
            public string[] gameSessionIds {get; set;}
            public string[] creatorIds {get; set;}
            public bool[] isPrivate {get; set;}
        }

        public class GameSessionResponse
        {
            public string playerSessionId {get; set;}
            public string ipAddress {get; set;}
            public int port {get; set;}
        }
        public class CreateGameResponse : GameSessionResponse { }
        public class JoinGameResponse : GameSessionResponse { }
    }
}
