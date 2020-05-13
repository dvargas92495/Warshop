using System;

namespace WarshopCommon {
    public abstract class Command
    {
        public const byte UP = 0;
        public const byte LEFT = 1;
        public const byte DOWN = 2;
        public const byte RIGHT = 3;

        public const byte SPAWN_COMMAND_ID = 0;
        public const byte MOVE_COMMAND_ID = 1;
        public const byte ATTACK_COMMAND_ID = 2;
        public const byte SPECIAL_COMMAND_ID = 3;

        public static byte[] limit = new byte[]
        {
            GameConstants.DEFAULT_SPAWN_LIMIT,
            GameConstants.DEFAULT_MOVE_LIMIT,
            GameConstants.DEFAULT_ATTACK_LIMIT,
            GameConstants.DEFAULT_SPECIAL_LIMIT
        };
        public static byte[] power = new byte[]
        {
            GameConstants.DEFAULT_SPAWN_POWER,
            GameConstants.DEFAULT_MOVE_POWER,
            GameConstants.DEFAULT_ATTACK_POWER,
            GameConstants.DEFAULT_SPECIAL_POWER
        };

        public static byte[] TYPES = new byte[] {
            SPAWN_COMMAND_ID,
            MOVE_COMMAND_ID,
            ATTACK_COMMAND_ID,
            SPECIAL_COMMAND_ID
        };

        public static string[] byteToDirectionString = new string[]{"Up", "Left", "Down", "Right"};
        
        public static Tuple<int, int> DirectionToVector(byte dir)
        {
            switch (dir)
            {
                case UP:
                    return new Tuple<int, int>(0, 1);
                case DOWN:
                    return new Tuple<int, int>(0, -1);
                case LEFT:
                    return new Tuple<int, int>(-1, 0);
                case RIGHT:
                    return new Tuple<int, int>(1, 0);
                default:
                    return new Tuple<int, int>(0, 0);
            }
        }
        public static string GetDisplay(byte commandId)
        {
            switch (commandId)
            {
                case SPAWN_COMMAND_ID:
                    return typeof(Spawn).Name;
                case MOVE_COMMAND_ID:
                    return typeof(Move).Name;
                case ATTACK_COMMAND_ID:
                    return typeof(Attack).Name;
                case SPECIAL_COMMAND_ID:
                    return typeof(Special).Name;
                default:
                    return typeof(Command).Name;
            }
        }

        public short robotId { get; set; }
        public string owner { get; set; }
        public string display { get; set; }
        public byte direction { get; set; }
        public byte commandId { get; set; }
        public Command(byte dir, byte id)
        {
            direction = dir;
            commandId = id;
            display = GetDisplay(commandId);
        }
        
        /*
        public static Command Deserialize(string msg)
        {
            Command cmd = JsonSerializer.Deserialize<Command>(msg);
            switch(cmd.commandId)
            {
                case SPAWN_COMMAND_ID:
                    cmd = (Spawn)cmd;
                    break;
                case MOVE_COMMAND_ID:
                    cmd = (Move)cmd;
                    break;
                case ATTACK_COMMAND_ID:
                    cmd = (Attack)cmd;
                    break;
                case SPECIAL_COMMAND_ID:
                    cmd = (Special)cmd;
                    break;
                default:
                    throw new Exception("No Command To Deserialize of ID: " + cmd.commandId);
            }
            return cmd;
        }
        */

        public override string ToString()
        {
            return string.Format("{0}-{1} {2}", robotId, display, byteToDirectionString[direction]);
        }

        public class Spawn : Command
        {
            public Spawn(byte dir) : base(dir, SPAWN_COMMAND_ID) { }
        }

        public class Move : Command
        {
            public Move(byte dir) : base(dir, MOVE_COMMAND_ID) { }
        }

        public class Attack : Command
        {
            public Attack(byte dir) : base(dir, ATTACK_COMMAND_ID) { }
        }

        public class Special : Command
        {
            public Special(byte dir) : base(dir, SPECIAL_COMMAND_ID) { }
        }
    }
}
