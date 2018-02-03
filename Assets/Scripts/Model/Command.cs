using UnityEngine;
using UnityEngine.Networking;

public abstract class Command
{
    internal short robotId { get; set; }
    internal string owner { get; set; }
    public Command(){}
    public abstract void Serialize(NetworkWriter writer);
    public static Command Deserialize(NetworkReader reader)
    {
        byte commandId = reader.ReadByte();
        Command cmd;
        switch(commandId)
        {
            case Rotate.COMMAND_ID:
                cmd = Rotate.Deserialize(reader);
                break;
            case Move.COMMAND_ID:
                cmd = Move.Deserialize(reader);
                break;
            case Attack.COMMAND_ID:
                cmd = Attack.Deserialize(reader);
                break;
            case Special.COMMAND_ID:
                cmd = Special.Deserialize(reader);
                break;
            default:
                return null; //TODO: Throw an error
        }
        cmd.robotId = reader.ReadInt16();
        cmd.owner = reader.ReadString();
        return cmd;
    }

    internal static Vector2Int DirectionToVector(Direction d)
    {
        switch (d)
        {
            case Direction.UP:
                return Vector2Int.up;
            case Direction.DOWN:
                return Vector2Int.down;
            case Direction.LEFT:
                return Vector2Int.left;
            case Direction.RIGHT:
                return Vector2Int.right;
            default:
                return Vector2Int.zero;
        }
    }

    internal static Robot.Orientation DirectionToOrientation(Direction d)
    {
        switch (d)
        {
            case Direction.UP:
                return Robot.Orientation.NORTH;
            case Direction.DOWN:
                return Robot.Orientation.SOUTH;
            case Direction.LEFT:
                return Robot.Orientation.WEST;
            case Direction.RIGHT:
                return Robot.Orientation.EAST;
            default:
                return 0;
        }
    }

    public override string ToString()
    {
        return "Empty Command";
    }

    internal class Rotate : Command
    {
        internal const byte COMMAND_ID = 1;
        internal const string DISPLAY = "ROTATE";
        internal Direction direction { get; }
        public Rotate(Direction dir)
        {
            direction = dir;
        }
        public override string ToString()
        {
            return DISPLAY + " " + direction;
        }
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(COMMAND_ID);
            writer.Write((byte)direction);
            writer.Write(robotId);
            writer.Write(owner);
        }
        public new static Rotate Deserialize(NetworkReader reader)
        {
            return new Rotate((Direction)reader.ReadByte());
        }
}

    internal class Move : Command
    {
        internal const byte COMMAND_ID = 2;
        internal const string DISPLAY = "MOVE";
        internal Direction direction { get; }

        public Move(Direction dir)
        {
            direction = dir;
        }
        public override string ToString()
        {
            return DISPLAY + " " + direction;
        }
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(COMMAND_ID);
            writer.Write((byte)direction);
            writer.Write(robotId);
            writer.Write(owner);
        }
        public new static Move Deserialize(NetworkReader reader)
        {
            return new Move((Direction)reader.ReadByte());
        }
    }

    internal class Attack : Command
    {
        internal const byte COMMAND_ID = 3;
        internal const string DISPLAY = "ATTACK";

        public override string ToString()
        {
            return DISPLAY;
        }
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(COMMAND_ID);
            writer.Write(robotId);
            writer.Write(owner);
        }
        public new static Attack Deserialize(NetworkReader reader)
        {
            return new Attack();
        }
    }

    internal class Special : Command
    {
        internal const byte COMMAND_ID = 4;
        internal const string DISPLAY = "SPECIAL";
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(COMMAND_ID);
            writer.Write(robotId);
            writer.Write(owner);
        }
        public new static Special Deserialize(NetworkReader reader)
        {
            return new Special();
        }
    }

    public enum Direction
    {
        LEFT = 0,
        RIGHT = 1,
        UP = 2,
        DOWN = 3
    }
}
