public abstract class RobotCommand {
    public abstract string toString();
    public int id { get; set; }
    public string owner { get; set; }
    public bool isOpponent { get; set; }
}
