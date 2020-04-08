using UnityEngine;
using UnityEngine.Networking;

public class ResolveEvent : GameEvent
{
    internal const byte EVENT_ID = 12;
    public List<Tuple<short, Vector2Int>> robotIdToSpawn;
    public List<Tuple<short, Vector2Int>> robotIdToMove;
    public List<Tuple<short, short>> robotIdToHealth;

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
        writer.Write(robotIdToHealth.GetLength());
        robotIdToHealth.ForEach(t => { 
            writer.Write(t.GetLeft());
            writer.Write(t.GetRight());
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
        int damageLength = reader.ReadInt32();
        evt.robotIdToHealth = new List<Tuple<short, short>>();
        for (int i = 0; i < damageLength; i++)
        {
            short robotId = reader.ReadInt16();
            short damage = reader.ReadInt16();
            evt.robotIdToHealth.Add(new Tuple<short, short>(robotId, damage));
        }
        return evt;
    }

    public override string ToString()
    {
        return string.Format("{0}Resolved commands:\nSpawn - {1}\nMove - {2}\nHealth - {3}", base.ToString(), robotIdToSpawn, robotIdToMove, robotIdToHealth);
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj)) return false;
        ResolveEvent other = (ResolveEvent)obj;
        return other.robotIdToSpawn.Equals(robotIdToSpawn) 
          && other.robotIdToMove.Equals(robotIdToMove) 
          && other.robotIdToHealth.Equals(robotIdToHealth);
    }

    public override int GetHashCode()
    {
        return EVENT_ID;
    }
}
