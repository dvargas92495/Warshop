using UnityEngine;
using UnityEngine.Networking;

public class MissEvent : GameEvent
{
    internal const byte EVENT_ID = 6;
    internal short robotId;
    internal List<Vector2Int> locs;

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(EVENT_ID);
        writer.Write(robotId);
        writer.Write(locs.GetLength());
        locs.ForEach(l =>
        {
            writer.Write(l.x);
            writer.Write(l.y);
        });
    }

    public new static MissEvent Deserialize(NetworkReader reader)
    {
        MissEvent evt = new MissEvent();
        evt.robotId = reader.ReadInt16();
        int length = reader.ReadInt32();
        evt.locs = new List<Vector2Int>();
        for (int i = 0; i < length; i++)
        {
            evt.locs.Add(new Vector2Int(reader.ReadInt32(), reader.ReadInt32()));
        }
        return evt;
    }

    public override string ToString()
    {
        return string.Format("{0}Robot {1} missed attacks at {2}", base.ToString(), robotId, locs);
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj)) return false;
        MissEvent other = (MissEvent)obj;
        return robotId == other.robotId && locs.Equals(other.locs);
    }

    public override int GetHashCode()
    {
        return EVENT_ID;
    }
}
