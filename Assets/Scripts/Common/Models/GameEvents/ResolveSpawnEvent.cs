using UnityEngine;
using UnityEngine.Networking;

public class ResolveSpawnEvent  : GameEvent
{
    internal const byte EVENT_ID = 14;
    public List<Tuple<short, Vector2Int>> robotIdToSpawn;

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(EVENT_ID);
        writer.Write(robotIdToSpawn.GetLength());
        robotIdToSpawn.ForEach(p => { 
            writer.Write(p.GetLeft());
            writer.Write(p.GetRight().x);
            writer.Write(p.GetRight().y);
        });
    }

    public new static ResolveSpawnEvent Deserialize(NetworkReader reader)
    {
        ResolveSpawnEvent evt = new ResolveSpawnEvent();
        int length = reader.ReadInt32();
        evt.robotIdToSpawn = new List<Tuple<short, Vector2Int>>();
        for (int i = 0; i < length; i++)
        {
            short robotId = reader.ReadInt16();
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            Vector2Int spawnSpace = new Vector2Int(x,y);
            Tuple<short, Vector2Int> pair = new Tuple<short, Vector2Int>(robotId, spawnSpace);
            evt.robotIdToSpawn.Add(pair);
        }
        return evt;
    }

    public override string ToString()
    {
        return string.Format("{0}Resolved spawns {1}", base.ToString(), robotIdToSpawn.ToString());
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj)) return false;
        ResolveSpawnEvent other = (ResolveSpawnEvent)obj;
        return other.robotIdToSpawn.Equals(robotIdToSpawn);
    }

    public override int GetHashCode()
    {
        return EVENT_ID;
    }
}

