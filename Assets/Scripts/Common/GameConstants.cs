public class GameConstants
{
    internal const int MAX_ROBOTS_ON_SQUAD = 4;
    internal const int MAX_STARS_ON_SQUAD = 8;

    public const byte DEFAULT_SPAWN_POWER = 2;
    public const byte DEFAULT_MOVE_POWER = 1;
    public const byte DEFAULT_ATTACK_POWER = 2;
    public const byte DEFAULT_SPECIAL_POWER = 2;

    public const byte DEFAULT_SPAWN_LIMIT = 1;
    public const byte DEFAULT_MOVE_LIMIT = 2;
    public const byte DEFAULT_ATTACK_LIMIT = 1;
    public const byte DEFAULT_SPECIAL_LIMIT = 0;

    public const byte DEFAULT_BATTERY_MULTIPLIER = 8;
    internal const byte DEFAULT_DEATH_MULTIPLIER = 8;

    internal const byte MAX_PRIORITY = 8;
    public const int POINTS_TO_WIN = 256;

    internal const string FINISHED_EVENTS = "Finished Events, Submit Your Moves!";
    internal const string IM_WAITING = "Waiting for opponent to submit moves...";
    internal const string OPPONENT_WAITING = "Opponent is waiting for you to submit your moves...";

    internal const string APP_LOG_DIR = "C:\\Game\\logs";
    internal const string GATEWAY_URL = "https://o29y7usfvd.execute-api.us-west-2.amazonaws.com/PROD";
}
