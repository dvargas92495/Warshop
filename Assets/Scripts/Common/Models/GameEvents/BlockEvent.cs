using UnityEngine;
using UnityEngine.Networking;

public class BlockEvent : GameEvent
{
    internal const byte EVENT_ID = 4;
    public const string WALL = "Wall";
    public const string BATTERY = "Battery";
    internal string blockingObject;
    internal Vector2Int deniedPos;
    internal short robotId;

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(EVENT_ID);
        writer.Write(blockingObject);
        writer.Write(deniedPos.x);
        writer.Write(deniedPos.y);
        writer.Write(robotId);
    }

    public new static BlockEvent Deserialize(NetworkReader reader)
    {
        BlockEvent evt = new BlockEvent();
        evt.blockingObject = reader.ReadString();
        evt.deniedPos = new Vector2Int();
        evt.deniedPos.x = reader.ReadInt32();
        evt.deniedPos.y = reader.ReadInt32();
        evt.robotId = reader.ReadInt16();
        return evt;
    }

    public override string ToString()
    {
        return string.Format("{0}Robot {1} was blocked by {2} from {3}", base.ToString(), robotId, blockingObject, deniedPos);
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj)) return false;
        BlockEvent other = (BlockEvent)obj;
        return robotId == other.robotId && blockingObject.Equals(other.blockingObject) && deniedPos.Equals(other.deniedPos);
    }

    public override int GetHashCode()
    {
        return EVENT_ID;
    }
}
