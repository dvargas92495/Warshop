using UnityEngine;
using UnityEngine.Networking;

public class ResolveMoveEvent  : GameEvent
{
    internal const byte EVENT_ID = 15;
    public List<Tuple<short, Vector2Int>> robotIdToMove;

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(EVENT_ID);
        writer.Write(robotIdToMove.GetLength());
        robotIdToMove.ForEach(p => { 
            writer.Write(p.GetLeft());
            writer.Write(p.GetRight().x);
            writer.Write(p.GetRight().y);
        });
    }

    public new static ResolveMoveEvent Deserialize(NetworkReader reader)
    {
        ResolveMoveEvent evt = new ResolveMoveEvent();
        int length = reader.ReadInt32();
        evt.robotIdToMove = new List<Tuple<short, Vector2Int>>();
        for (int i = 0; i < length; i++)
        {
            short robotId = reader.ReadInt16();
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            Vector2Int spawnSpace = new Vector2Int(x,y);
            Tuple<short, Vector2Int> pair = new Tuple<short, Vector2Int>(robotId, spawnSpace);
            evt.robotIdToMove.Add(pair);
        }
        return evt;
    }

    public override string ToString()
    {
        return string.Format("{0}Resolved moves {1}", base.ToString(), robotIdToMove.ToString());
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj)) return false;
        ResolveSpawnEvent other = (ResolveSpawnEvent)obj;
        return other.robotIdToSpawn.Equals(robotIdToMove);
    }

    public override int GetHashCode()
    {
        return EVENT_ID;
    }
}

