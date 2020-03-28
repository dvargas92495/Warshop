using UnityEngine;
using UnityEngine.Networking;

public class AttackEvent : GameEvent
{
    internal const byte EVENT_ID = 3;
    public List<Vector2Int> locs;
    public short robotId;

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(EVENT_ID);
        writer.Write(locs.GetLength());
        locs.ForEach(l =>
        {
            writer.Write(l.x);
            writer.Write(l.y);
        });
        writer.Write(robotId);
    }

    public new static AttackEvent Deserialize(NetworkReader reader)
    {
        AttackEvent evt = new AttackEvent();
        int length = reader.ReadInt32();
        evt.locs = new List<Vector2Int>();
        for (int i = 0; i < length; i++)
        {
            evt.locs.Add(new Vector2Int(reader.ReadInt32(), reader.ReadInt32()));
        }
        evt.robotId = reader.ReadInt16();
        return evt;
    }

    public override string ToString()
    {
        return string.Format("{0}Robot {1} attacked {2}", base.ToString(), robotId, locs);
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj)) return false;
        AttackEvent other = (AttackEvent)obj;
        return robotId == other.robotId && locs.Equals(other.locs);
    }

    public override int GetHashCode()
    {
        return EVENT_ID;
    }
}
