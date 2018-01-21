using UnityEngine;
using UnityEngine.Networking;

public abstract class GameEvent
{
    internal short primaryRobotId;
    internal byte priority;
    internal short primaryBattery;
    internal short secondaryBattery;
    public GameEvent() { }
    public void FinishMessage(NetworkWriter writer)
    {
        writer.Write(primaryRobotId);
        writer.Write(priority);
        writer.Write(primaryBattery);
        writer.Write(secondaryBattery);
    }
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
            case Block.EVENT_ID:
                evt = Block.Deserialize(reader);
                break;
            case Push.EVENT_ID:
                evt = Push.Deserialize(reader);
                break;
            case Miss.EVENT_ID:
                evt = Miss.Deserialize(reader);
                break;
            case Battery.EVENT_ID:
                evt = Battery.Deserialize(reader);
                break;
            case Fail.EVENT_ID:
                evt = Fail.Deserialize(reader);
                break;
            case Death.EVENT_ID:
                evt = Death.Deserialize(reader);
                break;
            default:
                evt = new Empty();
                break;//TODO: Log an error
        }
        evt.primaryRobotId = reader.ReadInt16();
        evt.priority = reader.ReadByte();
        evt.primaryBattery = reader.ReadInt16();
        evt.secondaryBattery = reader.ReadInt16();
        return evt;
    }
    public override string ToString()
    {
        return "Empty Event";
    }
    public string ToString(string message)
    {
        return "Robot " + primaryRobotId + " " + message + " on priority " + priority + ". Battery: " + primaryBattery + "|" + secondaryBattery;
    }

    public class Empty : GameEvent
    {
        internal const byte EVENT_ID = 0;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
        }
    }

    public class Rotate : GameEvent
    {
        internal const byte EVENT_ID = 1;
        internal Robot.Orientation sourceDir;
        internal Robot.Orientation destinationDir;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write((byte)sourceDir);
            writer.Write((byte)destinationDir);
        }
        public new static Rotate Deserialize(NetworkReader reader)
        {
            Rotate rot = new Rotate();
            rot.sourceDir = (Robot.Orientation)reader.ReadByte();
            rot.destinationDir = (Robot.Orientation)reader.ReadByte();
            return rot;
        }
        public override string ToString()
        {
            return ToString("rotated from " + sourceDir + " to " + destinationDir);
        }
    }

    public class Move : GameEvent
    {
        internal const byte EVENT_ID = 2;
        internal Vector2Int sourcePos;
        internal Vector2Int destinationPos;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(sourcePos.x);
            writer.Write(sourcePos.y);
            writer.Write(destinationPos.x);
            writer.Write(destinationPos.y);
        }
        public new static Move Deserialize(NetworkReader reader)
        {
            Move evt = new Move();
            evt.sourcePos = new Vector2Int();
            evt.sourcePos.x = reader.ReadInt32();
            evt.sourcePos.y = reader.ReadInt32();
            evt.destinationPos = new Vector2Int();
            evt.destinationPos.x = reader.ReadInt32();
            evt.destinationPos.y = reader.ReadInt32();
            return evt;
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
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(victimIds.Length);
            for (int i = 0; i < victimIds.Length; i++)
            {
                writer.Write(victimIds[i]);
                writer.Write(victimHealth[i]);
            }
        }
        public new static Attack Deserialize(NetworkReader reader)
        {
            Attack evt = new Attack();
            int length = reader.ReadInt32();
            evt.victimIds = new short[length];
            evt.victimHealth = new short[length];
            for (int i = 0;i < length; i++)
            {
                evt.victimIds[i] = reader.ReadInt16();
                evt.victimHealth[i] = reader.ReadInt16();
            }
            return evt;
        }
        public override string ToString()
        {
            return ToString("attacked " + string.Join(",", victimIds) + " down to " + string.Join(",", victimHealth));
        }
    }

    public class Block : GameEvent
    {
        internal const byte EVENT_ID = 4;
        internal string blockingObject;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(blockingObject);
        }
        public new static Block Deserialize(NetworkReader reader)
        {
            Block evt = new Block();
            evt.blockingObject = reader.ReadString();
            return evt;
        }
        public override string ToString()
        {
            return ToString("blocked by " + blockingObject);
        }
    }

    public class Push : GameEvent
    {
        internal const byte EVENT_ID = 5;
        internal Vector2Int sourcePos;
        internal Vector2Int transferPos;
        internal Vector2Int destinationPos;
        internal short victim;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(sourcePos.x);
            writer.Write(sourcePos.y);
            writer.Write(transferPos.x);
            writer.Write(transferPos.y);
            writer.Write(destinationPos.x);
            writer.Write(destinationPos.y);
            writer.Write(victim);
        }
        public new static Push Deserialize(NetworkReader reader)
        {
            Push evt = new Push();
            evt.sourcePos = new Vector2Int();
            evt.sourcePos.x = reader.ReadInt32();
            evt.sourcePos.y = reader.ReadInt32();
            evt.transferPos = new Vector2Int();
            evt.transferPos.x = reader.ReadInt32();
            evt.transferPos.y = reader.ReadInt32();
            evt.destinationPos = new Vector2Int();
            evt.destinationPos.x = reader.ReadInt32();
            evt.destinationPos.y = reader.ReadInt32();
            evt.victim = reader.ReadInt16();
            return evt;
        }
        public override string ToString()
        {
            return ToString("pushed " + victim + " from " + sourcePos + " through " + transferPos + " to " + destinationPos);
        }
    }

    public class Miss : GameEvent
    {
        internal const byte EVENT_ID = 6;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
        }
        public new static Miss Deserialize(NetworkReader reader)
        {
            Miss evt = new Miss();
            return evt;
        }
        public override string ToString()
        {
            return ToString("attacked but missed");
        }
    }

    public class Battery : GameEvent
    {
        internal const byte EVENT_ID = 7;
        internal short damage;
        internal bool opponentsBase;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(damage);
            writer.Write(opponentsBase);
        }
        public new static Battery Deserialize(NetworkReader reader)
        {
            Battery evt = new Battery();
            evt.damage = reader.ReadInt16();
            evt.opponentsBase = reader.ReadBoolean();
            return evt;
        }
        public override string ToString()
        {
            return ToString("attacked " + (opponentsBase ? "opponent's":"its own") + " battery with " + damage + " damage.");
        }
    }

    public class Fail : GameEvent
    {
        internal const byte EVENT_ID = 8;
        internal string failedCmd;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(failedCmd);
        }
        public new static Fail Deserialize(NetworkReader reader)
        {
            Fail evt = new Fail();
            evt.failedCmd = reader.ReadString();
            return evt;
        }
        public override string ToString()
        {
            return ToString("failed to execute " + failedCmd + " due to limit.");
        }
    }

    public class Death: GameEvent
    {
        internal const byte EVENT_ID = 9;
        internal Vector2Int returnLocation;
        internal short returnHealth;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(returnLocation.x);
            writer.Write(returnLocation.y);
            writer.Write(returnHealth);
        }
        public new static Death Deserialize(NetworkReader reader)
        {
            Death evt = new Death();
            evt.returnLocation = new Vector2Int();
            evt.returnLocation.x = reader.ReadInt32();
            evt.returnLocation.y = reader.ReadInt32();
            evt.returnHealth = reader.ReadInt16();
            return evt;
        }
        public override string ToString()
        {
            return ToString("dies and returns to queue");
        }
    }

}
