using UnityEngine;
using UnityEngine.Networking;

public abstract class GameEvent
{
    public byte priority;
    public short primaryBatteryCost;
    public short secondaryBatteryCost;

    public void FinishMessage(NetworkWriter writer)
    {
        writer.Write(priority);
        writer.Write(primaryBatteryCost);
        writer.Write(secondaryBatteryCost);
    }
    public abstract void Serialize(NetworkWriter writer);
    public static GameEvent Deserialize(NetworkReader reader)
    {
        byte eventId = reader.ReadByte();
        GameEvent evt;
        switch (eventId)
        {
            case SpawnEvent.EVENT_ID:
                evt = SpawnEvent.Deserialize(reader);
                break;
            case MoveEvent.EVENT_ID:
                evt = MoveEvent.Deserialize(reader);
                break;
            case AttackEvent.EVENT_ID:
                evt = AttackEvent.Deserialize(reader);
                break;
            case PushEvent.EVENT_ID:
                evt = PushEvent.Deserialize(reader);
                break;
            case Death.EVENT_ID:
                evt = Death.Deserialize(reader);
                break;
            case ResolveEvent.EVENT_ID:
                evt = ResolveEvent.Deserialize(reader);
                break;
            case EndEvent.EVENT_ID:
                evt = EndEvent.Deserialize(reader);
                break;
            default:
                throw new ZException("Unknown Event Id to deserialize: " + eventId);
        }
        evt.priority = reader.ReadByte();
        evt.primaryBatteryCost = reader.ReadInt16();
        evt.secondaryBatteryCost = reader.ReadInt16();
        return evt;
    }
    public override string ToString()
    {
        return string.Format("Event at {0} costing ({1},{2}) - ", priority, primaryBatteryCost, secondaryBatteryCost);
    }
    public override bool Equals(object obj)
    {
        if (!GetType().Equals(obj.GetType())) return false;
        GameEvent other = (GameEvent)obj;
        return priority == other.priority &&
            primaryBatteryCost == other.primaryBatteryCost &&
            secondaryBatteryCost == other.secondaryBatteryCost;
    }
    public void Flip()
    {
        short battery = primaryBatteryCost;
        primaryBatteryCost = secondaryBatteryCost;
        secondaryBatteryCost = battery;
    }
    public class Empty : GameEvent
    {
        internal const byte EVENT_ID = 0;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
        }
    }

    // OPEN ID AT 8

    public class Death: GameEvent
    {
        internal const byte EVENT_ID = 9;
        internal short robotId;
        internal short returnHealth;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(robotId);
            writer.Write(returnHealth);
        }
        public new static Death Deserialize(NetworkReader reader)
        {
            Death evt = new Death();
            evt.robotId = reader.ReadInt16();
            evt.returnHealth = reader.ReadInt16();
            return evt;
        }
        public override string ToString()
        {
            return string.Format("{0}Robot {1} died and returned to {2} health", base.ToString(), robotId, returnHealth);
        }
    }
}
