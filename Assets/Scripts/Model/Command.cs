using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Command
{
    internal const byte UP = 0;
    internal const byte DOWN = 1;
    internal const byte LEFT = 2;
    internal const byte RIGHT = 3;
    internal static Dictionary<byte, string> tostring = new Dictionary<byte, string>()
    {
        {UP, "Up" },
        {DOWN, "Down" },
        {LEFT, "Left" },
        {RIGHT, "Right" }
    };

    internal short robotId { get; set; }
    internal string owner { get; set; }
    protected internal byte direction { get; protected set; }
    internal static Dictionary<byte, Type> byteToCmd = new Dictionary<byte, Type>()
    {
        {1, typeof(Move) },
        {2, typeof(Attack) },
        {3, typeof(Special) }
    };
    public Command(){}
    public abstract void Serialize(NetworkWriter writer);
    public static Command Deserialize(NetworkReader reader)
    {
        byte commandId = reader.ReadByte();
        Command cmd;
        switch(commandId)
        {
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

    public override string ToString()
    {
        return "Empty Command";
    }

    internal class Move : Command
    {
        internal const byte COMMAND_ID = 2;
        internal const string DISPLAY = "Move";

        public Move(byte dir)
        {
            direction = dir;
        }
        public override string ToString()
        {
            return DISPLAY + " " + tostring[direction];
        }
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(COMMAND_ID);
            writer.Write(direction);
            writer.Write(robotId);
            writer.Write(owner);
        }
        public new static Move Deserialize(NetworkReader reader)
        {
            return new Move(reader.ReadByte());
        }

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
    }

    internal class Attack : Command
    {
        internal const byte COMMAND_ID = 3;
        internal const string DISPLAY = "Attack";

        public override string ToString()
        {
            return DISPLAY + " Arrow";
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
}
