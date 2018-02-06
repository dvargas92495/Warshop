internal class GameConstants
{
    internal const byte DEFAULT_ROTATE_POWER = 1;
    internal const byte DEFAULT_MOVE_POWER = 1;
    internal const byte DEFAULT_ATTACK_POWER = 2;
    internal const byte DEFAULT_SPECIAL_POWER = 2;

    internal const byte DEFAULT_ROTATE_LIMIT = 2;
    internal const byte DEFAULT_MOVE_LIMIT = 2;
    internal const byte DEFAULT_ATTACK_LIMIT = 1;
    internal const byte DEFAULT_SPECIAL_LIMIT = 1;

    internal const byte DEFAULT_BATTERY_MULTIPLIER = 8;
    internal const byte DEFAULT_DEATH_MULTIPLIER = 8;

    internal const byte MAX_PRIORITY = 8;
    internal const int POINTS_TO_WIN = 256;
    internal const float ROBOTZ = -0.25f;
    internal const string FINISHED_EVENTS = "Finished Events, Submit Your Moves!";
    internal const string IM_WAITING = "Waiting for opponent to submit moves...";
    internal const string OPPONENT_WAITING = "Opponent is waiting for you to submit your moves...";

    internal const string APP_LOG_DIR = "/local/game/logs";
    internal const string APP_ERROR_DIR = "/local/game/error";
    internal static string PRODUCTION_ALIAS = "Z8_App";
    internal class GAME_SESSION_PROPERTIES {
        internal const string BOARDFILE = "boardFile";
    }

    //TODO: Bring these out to a config file
    internal static bool LOCAL_MODE = true;
    internal static bool USE_SERVER = false;
    internal static string AWS_PUBLIC_KEY = "public key";
    internal static string AWS_SECRET_KEY = "secret key";
}
