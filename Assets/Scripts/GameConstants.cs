public class GameConstants
{
    public const byte DEFAULT_ROTATE_POWER = 1;
    public const byte DEFAULT_MOVE_POWER = 1;
    public const byte DEFAULT_ATTACK_POWER = 2;
    public const byte DEFAULT_SPECIAL_POWER = 2;

    public const byte DEFAULT_ROTATE_LIMIT = 2;
    public const byte DEFAULT_MOVE_LIMIT = 2;
    public const byte DEFAULT_ATTACK_LIMIT = 1;
    public const byte DEFAULT_SPECIAL_LIMIT = 1;

    public const byte DEFAULT_BATTERY_MULTIPLIER = 8;
    public const byte DEFAULT_DEATH_MULTIPLIER = 8;

    public const byte MAX_PRIORITY = 8;
    public const int POINTS_TO_WIN = 256;
    public const float ROBOTZ = 0.25f;
    public const string APP_LOG_DIR = "/local/game/logs";
    public const string APP_ERROR_DIR = "/local/game/error";
    public class GAME_SESSION_PROPERTIES {
        public const string BOARDFILE = "boardFile";
    }

    //TODO: Bring these out to a config file
    public static bool LOCAL_MODE = true;
    public static bool USE_SERVER = false;
    public static string SERVER_IP = "127.0.0.1";
    public static string FLEET_ID = "fleet-40d69432-dc29-4994-ac77-1288529d6efa";
    public static int PORT = 12345;
}
