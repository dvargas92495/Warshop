using UnityEngine;
using UnityEngine.Networking;

public abstract class Command
{
    public const byte UP = 0;
    public const byte LEFT = 1;
    public const byte DOWN = 2;
    public const byte RIGHT = 3;

    internal const byte SPAWN_COMMAND_ID = 0;
    internal const byte MOVE_COMMAND_ID = 1;
    internal const byte ATTACK_COMMAND_ID = 2;
    internal const byte SPECIAL_COMMAND_ID = 3;

    internal const byte NUM_TYPES = 4;

    internal static string[] byteToDirectionString = new string[]{"Up", "Left", "Down", "Right"};

    internal static byte[] limit = new byte[] 
    {
        GameConstants.DEFAULT_SPAWN_LIMIT,
        GameConstants.DEFAULT_MOVE_LIMIT,
        GameConstants.DEFAULT_ATTACK_LIMIT,
        GameConstants.DEFAULT_SPECIAL_LIMIT
    };
    internal static byte[] power = new byte[]
    {
        GameConstants.DEFAULT_SPAWN_POWER,
        GameConstants.DEFAULT_MOVE_POWER,
        GameConstants.DEFAULT_ATTACK_POWER,
        GameConstants.DEFAULT_SPECIAL_POWER
    };
    internal static Vector2Int DirectionToVector(byte dir)
    {
        switch (dir)
        {
            case UP:
                return Vector2Int.up;
            case DOWN:
                return Vector2Int.down;
            case LEFT:
                return Vector2Int.left;
            case RIGHT:
                return Vector2Int.right;
            default:
                return Vector2Int.zero;
        }
    }
    internal static string GetDisplay(byte commandId)
    {
        switch (commandId)
        {
            case SPAWN_COMMAND_ID:
                return "Spawn";
            case MOVE_COMMAND_ID:
                return "Move";
            case ATTACK_COMMAND_ID:
                return "Attack";
            case SPECIAL_COMMAND_ID:
                return "Special";
            default:
                return "Invalid";
        }
    }

    public short robotId { get; set; }
    internal string owner { get; set; }
    protected internal string display { get; protected set; }
    protected internal byte direction { get; protected set; }
    protected internal byte commandId { get; protected set; }
    public Command(){ }
    public void Serialize(NetworkWriter writer)
    {
        writer.Write(commandId);
        writer.Write(direction);
        writer.Write(robotId);
        writer.Write(owner);
    }
    public static Command Deserialize(NetworkReader reader)
    {
        byte commandId = reader.ReadByte();
        byte dir = reader.ReadByte();
        Command cmd;
        switch(commandId)
        {
            case SPAWN_COMMAND_ID:
                cmd = new Spawn(dir);
                break;
            case MOVE_COMMAND_ID:
                cmd = new Move(dir);
                break;
            case ATTACK_COMMAND_ID:
                cmd = new Attack(dir);
                break;
            case SPECIAL_COMMAND_ID:
                cmd = new Special(dir);
                break;
            default:
                throw new ZException("No Command To Deserialize of ID: " + commandId);
        }
        cmd.robotId = reader.ReadInt16();
        cmd.owner = reader.ReadString();
        return cmd;
    }

    public override string ToString()
    {
        return display + " " + byteToDirectionString[direction];
    }

    public class Spawn : Command
    {
        public Spawn(byte dir)
        {
            direction = dir;
            commandId = SPAWN_COMMAND_ID;
            display = GetDisplay(commandId);
        }
    }

    public class Move : Command
    {
        public Move(byte dir)
        {
            direction = dir;
            commandId = MOVE_COMMAND_ID;
            display = GetDisplay(commandId);
        }
    }

    public class Attack : Command
    {
        public Attack(byte dir)
        {
            direction = dir;
            commandId = ATTACK_COMMAND_ID;
            display = GetDisplay(commandId);
        }
    }

    internal class Special : Command
    {
        public Special(byte dir)
        {
            direction = dir;
            commandId = SPECIAL_COMMAND_ID;
            display = GetDisplay(commandId);
        }
    }
}
