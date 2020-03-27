using UnityEngine;
using UnityEngine.Networking;

public class SpawnEvent : GameEvent
{
    internal const byte EVENT_ID = 1;
    public Vector2Int destinationPos;
    public short robotId;

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(EVENT_ID);
        writer.Write(destinationPos.x);
        writer.Write(destinationPos.y);
        writer.Write(robotId);
    }

    public new static SpawnEvent Deserialize(NetworkReader reader)
    {
        SpawnEvent evt = new SpawnEvent();
        evt.destinationPos = new Vector2Int();
        evt.destinationPos.x = reader.ReadInt32();
        evt.destinationPos.y = reader.ReadInt32();
        evt.robotId = reader.ReadInt16();
        return evt;
    }

    public override string ToString()
    {
        return string.Format("{0}Robot {1} spawned at {2}", base.ToString(), robotId, destinationPos);
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj)) return false;
        SpawnEvent other = (SpawnEvent)obj;
        return destinationPos.Equals(other.destinationPos) &&
        robotId == other.robotId;
    }

    public override int GetHashCode()
    {
        return EVENT_ID;
    }
}
