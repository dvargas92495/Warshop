using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public abstract class Command
{
    internal const byte UP = 0;
    internal const byte LEFT = 1;
    internal const byte DOWN = 2;
    internal const byte RIGHT = 3;
    internal static Dictionary<byte, string> byteToDirectionString = new Dictionary<byte, string>()
    {
        {UP, "Up" },
        {DOWN, "Down" },
        {LEFT, "Left" },
        {RIGHT, "Right" }
    };
    internal static Dictionary<byte, Type> byteToCmd = new Dictionary<byte, Type>()
    {
        {Move.COMMAND_ID, typeof(Move) },
        {Attack.COMMAND_ID, typeof(Attack) },
        {Special.COMMAND_ID, typeof(Special) }
    };
    static internal Dictionary<Type, byte> limit = new Dictionary<Type, byte>()
    {
        { typeof(Move), GameConstants.DEFAULT_MOVE_LIMIT },
        { typeof(Attack), GameConstants.DEFAULT_ATTACK_LIMIT },
        { typeof(Special), GameConstants.DEFAULT_SPECIAL_LIMIT }
    };
    static internal Dictionary<Type, byte> power = new Dictionary<Type, byte>()
    {
        { typeof(Move), GameConstants.DEFAULT_MOVE_POWER },
        { typeof(Attack), GameConstants.DEFAULT_ATTACK_POWER },
        { typeof(Special), GameConstants.DEFAULT_SPECIAL_POWER }
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

    internal short robotId { get; set; }
    internal string owner { get; set; }
    protected internal byte direction { get; protected set; }
    public Command(){ }
    public virtual string ToSpriteString()
    {
        return "NULL";
    }
    public virtual void Serialize(NetworkWriter writer)
    {
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
            case Move.COMMAND_ID:
                cmd = new Move(dir);
                break;
            case Attack.COMMAND_ID:
                cmd = new Attack(dir);
                break;
            case Special.COMMAND_ID:
                cmd = new Special(dir);
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
        internal const byte COMMAND_ID = 1;
        internal const string DISPLAY = "Move";

        public Move(byte dir)
        {
            direction = dir;
        }
        public override string ToSpriteString()
        {
            return DISPLAY + " Arrow";
        }
        public override string ToString()
        {
            return DISPLAY + " " + byteToDirectionString[direction];
        }
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(COMMAND_ID);
            base.Serialize(writer);
        }
    }

    internal class Attack : Command
    {
        internal const byte COMMAND_ID = 2;
        internal const string DISPLAY = "Attack";

        public Attack(byte dir)
        {
            direction = dir;
        }
        public override string ToSpriteString()
        {
            return DISPLAY + " Arrow";
        }
        public override string ToString()
        {
            return DISPLAY + " " + byteToDirectionString[direction];
        }
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(COMMAND_ID);
            base.Serialize(writer);
        }
    }

    internal class Special : Command
    {
        internal const byte COMMAND_ID = 3;
        internal const string DISPLAY = "SPECIAL";

        public Special(byte dir)
        {
            direction = dir;
        }
        public override string ToSpriteString()
        {
            return DISPLAY + " Arrow";
        }
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(COMMAND_ID);
            base.Serialize(writer);
        }
    }
}
