using UnityEngine;
using UnityEngine.Networking;

public class MoveEvent : GameEvent
{
    internal const byte EVENT_ID = 2;
    internal Vector2Int sourcePos;
    internal Vector2Int destinationPos;
    internal short robotId;

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(EVENT_ID);
        writer.Write(sourcePos.x);
        writer.Write(sourcePos.y);
        writer.Write(destinationPos.x);
        writer.Write(destinationPos.y);
        writer.Write(robotId);
    }
    public new static MoveEvent Deserialize(NetworkReader reader)
    {
        MoveEvent evt = new MoveEvent();
        evt.sourcePos = new Vector2Int();
        evt.sourcePos.x = reader.ReadInt32();
        evt.sourcePos.y = reader.ReadInt32();
        evt.destinationPos = new Vector2Int();
        evt.destinationPos.x = reader.ReadInt32();
        evt.destinationPos.y = reader.ReadInt32();
        evt.robotId = reader.ReadInt16();
        return evt;
    }
    public override string ToString()
    {
        return string.Format("{0}Robot {1} moved from {2} to {3}", base.ToString(), robotId, sourcePos, destinationPos);
    }
}
