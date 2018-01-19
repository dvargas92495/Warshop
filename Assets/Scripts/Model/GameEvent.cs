using UnityEngine;
using UnityEngine.Networking;

public abstract class GameEvent
{
    internal short primaryRobotId;
    internal byte priority;
    public GameEvent() { }
    public abstract void Serialize(NetworkWriter writer);
    public static GameEvent Deserialize(NetworkReader reader)
    {
        byte eventId = reader.ReadByte();
        GameEvent evt;
        switch (eventId)
        {
            case Rotate.EVENT_ID:
                evt = Rotate.Deserialize(reader);
                break;
            case Move.EVENT_ID:
                evt = Move.Deserialize(reader);
                break;
            case Attack.EVENT_ID:
                evt = Attack.Deserialize(reader);
                break;
            default:
                return null; //TODO: Throw an error
        }
        evt.primaryRobotId = reader.ReadInt16();
        evt.priority = reader.ReadByte();
        return evt;
    }
    public override string ToString()
    {
        return "Empty Event";
    }
    public string ToString(string message)
    {
        return "Robot " + primaryRobotId + " " + message + " on priority " + priority;
    }

    public class Rotate : GameEvent
    {
        internal const byte EVENT_ID = 1;
        internal Robot.Orientation sourceDir { get; }
        internal Robot.Orientation destinationDir { get; }
        internal Rotate(Robot robot, Command.Direction dir)
        {
            sourceDir = robot.orientation;
            switch (dir)
            {
                case Command.Direction.UP:
                    robot.orientation = Robot.Orientation.NORTH;
                    break;
                case Command.Direction.DOWN:
                    robot.orientation = Robot.Orientation.SOUTH;
                    break;
                case Command.Direction.LEFT:
                    robot.orientation = Robot.Orientation.WEST;
                    break;
                case Command.Direction.RIGHT:
                    robot.orientation = Robot.Orientation.EAST;
                    break;
            }
            destinationDir = robot.orientation;
            primaryRobotId = robot.id;
        }
        private Rotate(byte source, byte dest)
        {
            sourceDir = (Robot.Orientation)source;
            destinationDir = (Robot.Orientation)dest;
        }
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write((byte)sourceDir);
            writer.Write((byte)destinationDir);
            writer.Write(primaryRobotId);
            writer.Write(priority);
        }
        public new static Rotate Deserialize(NetworkReader reader)
        {
            byte source = reader.ReadByte();
            byte dest = reader.ReadByte();
            return new Rotate(source, dest);
        }
        public override string ToString()
        {
            return ToString("rotated from " + sourceDir + " to " + destinationDir);
        }
    }

    public class Move : GameEvent
    {
        internal const byte EVENT_ID = 2;
        internal Vector2 sourcePos { get; }
        internal Vector2 destinationPos { get; }
        internal Move(Robot robot, Command.Direction dir)
        {
            sourcePos = robot.position;
            switch (dir)
            {
                case Command.Direction.UP:
                    robot.position += new Vector2(0, -1);
                    break;
                case Command.Direction.DOWN:
                    robot.position += new Vector2(0, 1);
                    break;
                case Command.Direction.LEFT:
                    robot.position += new Vector2(-1, 0);
                    break;
                case Command.Direction.RIGHT:
                    robot.position += new Vector2(1, 0);
                    break;
            }
            destinationPos = robot.position;
            primaryRobotId = robot.id;
        }
        private Move(Vector2 source, Vector2 dest)
        {
            sourcePos = source;
            destinationPos = dest;
        }
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(sourcePos);
            writer.Write(destinationPos);
            writer.Write(primaryRobotId);
            writer.Write(priority);
        }
        public new static Move Deserialize(NetworkReader reader)
        {
            Vector2 source = reader.ReadVector2();
            Vector2 dest = reader.ReadVector2();
            return new Move(source, dest);
        }
        public override string ToString()
        {
            return ToString("moved from " + sourcePos + " to " + destinationPos);
        }
    }

    public class Attack : GameEvent
    {
        internal const byte EVENT_ID = 3;
        internal short[] victimIds;
        internal short[] victimHealth;
        internal Attack(Robot robot, Robot[] victims)
        {
            victimIds = new short[victims.Length];
            victimHealth = new short[victims.Length];
            for(int i = 0; i < victims.Length; i++)
            {
                Robot victim = victims[i];
                victimIds[i] = victim.id;
                victim.health -= robot.attack;
                victimHealth[i] = victim.health;
            }
            primaryRobotId = robot.id;
        }
        private Attack(short[] vids, short[] vhealths)
        {
            victimIds = vids;
            victimHealth = vhealths;
        }
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(victimIds.Length);
            for (int i = 0; i < victimIds.Length; i++)
            {
                writer.Write(victimIds[i]);
                writer.Write(victimHealth[i]);
            }
            writer.Write(primaryRobotId);
            writer.Write(priority);
        }
        public new static Attack Deserialize(NetworkReader reader)
        {
            int length = reader.ReadInt32();
            short[] vids = new short[length];
            short[] vhealths = new short[length];
            for (int i = 0;i < length; i++)
            {
                vids[i] = reader.ReadInt16();
                vhealths[i] = reader.ReadInt16();
            }
            return new Attack(vids, vhealths);
        }
        public override string ToString()
        {
            return ToString("attacked " + string.Join(",", victimIds) + " down to " + string.Join(",", victimHealth));
        }
    }

}
