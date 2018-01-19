public class GameConstants {
    public const int MAX_POWER = 8;
    public const byte MAX_PRIORITY = 8;
    public const int POINTS_TO_WIN = 256;
    public const string RESOURCES = "Assets/Resources/";
    public const string ROBOT_PREFAB_DIR = "Robots/Models/";
    public const float ROBOTZ = 0.25f;
    public const string BOARDFILE_DIR = "Game/BoardFiles/";
    public const string TILE_MATERIAL_DIR = "Game/Tiles/Materials/";
    public const string LOCAL_SERVER_IP = "127.0.0.1";
    public const string APP_LOG_DIR = "/local/game/logs";
    public const string APP_ERROR_DIR = "/local/game/error";

    public static bool LOCAL_MODE = true;
    public static bool USE_SERVER = false;
    public static string SERVER_IP = "35.163.93.183";
    public static int PORT = 12345;
}
