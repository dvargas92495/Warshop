using UnityEngine;
using UnityEngine.Networking;

public class ResolveEvent : GameEvent
{
    internal const byte EVENT_ID = 12;
    public List<Tuple<short, Vector2Int>> robotIdToSpawn;
    public List<Tuple<short, Vector2Int>> robotIdToMove;

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(EVENT_ID);
        writer.Write(robotIdToSpawn.GetLength());
        robotIdToSpawn.ForEach(p => { 
            writer.Write(p.GetLeft());
            writer.Write(p.GetRight().x);
            writer.Write(p.GetRight().y);
        });
        writer.Write(robotIdToMove.GetLength());
        robotIdToMove.ForEach(p => { 
            writer.Write(p.GetLeft());
            writer.Write(p.GetRight().x);
            writer.Write(p.GetRight().y);
        });
    }

    public new static ResolveEvent Deserialize(NetworkReader reader)
    {
        ResolveEvent evt = new ResolveEvent();
        int spawnLength = reader.ReadInt32();
        evt.robotIdToSpawn = new List<Tuple<short, Vector2Int>>();
        for (int i = 0; i < spawnLength; i++)
        {
            short robotId = reader.ReadInt16();
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            Vector2Int spawnSpace = new Vector2Int(x,y);
            Tuple<short, Vector2Int> pair = new Tuple<short, Vector2Int>(robotId, spawnSpace);
            evt.robotIdToSpawn.Add(pair);
        }
        int moveLength = reader.ReadInt32();
        evt.robotIdToMove = new List<Tuple<short, Vector2Int>>();
        for (int i = 0; i < moveLength; i++)
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
        return string.Format("{0}Resolved commands:\n{1}\n{2}", base.ToString(), robotIdToSpawn, robotIdToMove);
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj)) return false;
        ResolveEvent other = (ResolveEvent)obj;
        return other.robotIdToSpawn.Equals(robotIdToSpawn) && other.robotIdToMove.Equals(robotIdToMove);
    }

    public override int GetHashCode()
    {
        return EVENT_ID;
    }
}
