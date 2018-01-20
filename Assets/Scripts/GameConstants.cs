﻿public class GameConstants {
    public const byte MAX_PRIORITY = 8;
    public const int POINTS_TO_WIN = 256;
    public const float ROBOTZ = 0.25f;
    public const string LOCAL_SERVER_IP = "127.0.0.1";
    public const string APP_LOG_DIR = "/local/game/logs";
    public const string APP_ERROR_DIR = "/local/game/error";

    //TODO: Bring these out to a config file
    public static bool LOCAL_MODE = true;
    public static bool USE_SERVER = false;
    public static string SERVER_IP = "54.202.247.134";
    public static int PORT = 12345;
}
