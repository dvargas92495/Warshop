using UnityEngine;
using UnityEngine.Networking;

public class PushEvent : GameEvent
{
    internal const byte EVENT_ID = 5;
    internal short robotId;
    internal short victim;
    internal Vector2Int direction;

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(EVENT_ID);
        writer.Write(robotId);
        writer.Write(victim);
        writer.Write(direction.x);
        writer.Write(direction.y);
    }

    public new static PushEvent Deserialize(NetworkReader reader)
    {
        PushEvent evt = new PushEvent();
        evt.robotId = reader.ReadInt16();
        evt.victim = reader.ReadInt16();
        evt.direction = new Vector2Int();
        evt.direction.x = reader.ReadInt32();
        evt.direction.y = reader.ReadInt32();
        return evt;
    }

    public override string ToString()
    {
        return string.Format("{0}Robot {1} pushed {2} towards {3}", base.ToString(), robotId, victim, direction);
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj)) return false;
        PushEvent other = (PushEvent)obj;
        return other.robotId == robotId && victim == other.victim && direction.Equals(other.direction);
    }
}
