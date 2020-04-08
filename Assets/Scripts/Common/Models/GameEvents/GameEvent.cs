using UnityEngine;
using UnityEngine.Networking;

public abstract class GameEvent
{
    public byte priority;
    public short primaryBatteryCost;
    public short secondaryBatteryCost;
    public bool success = true;

    public void FinishMessage(NetworkWriter writer)
    {
        writer.Write(priority);
        writer.Write(primaryBatteryCost);
        writer.Write(secondaryBatteryCost);
        writer.Write(success);
    }
    public abstract void Serialize(NetworkWriter writer);
    public static GameEvent Deserialize(NetworkReader reader)
    {
        byte eventId = reader.ReadByte();
        GameEvent evt;
        switch (eventId)
        {
            case SpawnEvent.EVENT_ID:
                evt = SpawnEvent.Deserialize(reader);
                break;
            case MoveEvent.EVENT_ID:
                evt = MoveEvent.Deserialize(reader);
                break;
            case AttackEvent.EVENT_ID:
                evt = AttackEvent.Deserialize(reader);
                break;
            case BlockEvent.EVENT_ID:
                evt = BlockEvent.Deserialize(reader);
                break;
            case PushEvent.EVENT_ID:
                evt = PushEvent.Deserialize(reader);
                break;
            case MissEvent.EVENT_ID:
                evt = MissEvent.Deserialize(reader);
                break;
            case BatteryEvent.EVENT_ID:
                evt = BatteryEvent.Deserialize(reader);
                break;
            case Collision.EVENT_ID:
                evt = End.Deserialize(reader);
                break;
            case Death.EVENT_ID:
                evt = Death.Deserialize(reader);
                break;
            case ResolveEvent.EVENT_ID:
                evt = ResolveEvent.Deserialize(reader);
                break;
            case End.EVENT_ID:
                evt = End.Deserialize(reader);
                break;
            default:
                throw new ZException("Unknown Event Id to deserialize: " + eventId);
        }
        evt.priority = reader.ReadByte();
        evt.primaryBatteryCost = reader.ReadInt16();
        evt.secondaryBatteryCost = reader.ReadInt16();
        evt.success = reader.ReadBoolean();
        return evt;
    }
    public override string ToString()
    {
        return string.Format("Event({3}) at {0} costing ({1},{2}) - ", priority, primaryBatteryCost, secondaryBatteryCost, success);
    }
    public override bool Equals(object obj)
    {
        if (!GetType().Equals(obj.GetType())) return false;
        GameEvent other = (GameEvent)obj;
        return priority == other.priority &&
            primaryBatteryCost == other.primaryBatteryCost &&
            secondaryBatteryCost == other.secondaryBatteryCost &&
            success == other.success;
    }
    public void Flip()
    {
        short battery = primaryBatteryCost;
        primaryBatteryCost = secondaryBatteryCost;
        secondaryBatteryCost = battery;
    }
    public class Empty : GameEvent
    {
        internal const byte EVENT_ID = 0;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
        }
    }

    // OPEN ID AT 8

    public class Death: GameEvent
    {
        internal const byte EVENT_ID = 9;
        internal short robotId;
        internal short returnHealth;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(robotId);
            writer.Write(returnHealth);
        }
        public new static Death Deserialize(NetworkReader reader)
        {
            Death evt = new Death();
            evt.robotId = reader.ReadInt16();
            evt.returnHealth = reader.ReadInt16();
            return evt;
        }
        public override string ToString()
        {
            return string.Format("{0}Robot {1} died and returned to {2} health", base.ToString(), robotId, returnHealth);
        }
    }

    public class End : GameEvent
    {
        internal const byte EVENT_ID = 13;
        internal bool primaryLost;
        internal bool secondaryLost;
        internal short turnCount;
        internal Dictionary<short, Game.RobotStat> primaryTeamStats;
        internal Dictionary<short, Game.RobotStat> secondaryTeamStats;

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(primaryLost);
            writer.Write(secondaryLost);
            writer.Write(turnCount);
            writer.Write(primaryTeamStats.GetLength());
            primaryTeamStats.ForEach((k, stat) =>
            {
                writer.Write(k);
                stat.Serialize(writer);
            });
            writer.Write(secondaryTeamStats.GetLength());
            secondaryTeamStats.ForEach((k, stat) =>
            {
                writer.Write(k);
                stat.Serialize(writer);
            });


        }
        public new static End Deserialize(NetworkReader reader)
        {
            End evt = new End();
            evt.primaryLost = reader.ReadBoolean();
            evt.secondaryLost = reader.ReadBoolean();
            evt.turnCount = reader.ReadInt16();
            evt.primaryTeamStats = new Dictionary<short, Game.RobotStat>(reader.ReadInt32());
            for (int i = 0; i < evt.primaryTeamStats.GetLength(); i++)
            {
                short k = reader.ReadInt16();
                Game.RobotStat stat = new Game.RobotStat();
                stat.Deserialize(reader);
                evt.primaryTeamStats.Add(k, stat);
            }
            evt.secondaryTeamStats = new Dictionary<short, Game.RobotStat>(reader.ReadInt32());
            for (int i = 0; i < evt.secondaryTeamStats.GetLength(); i++)
            {
                short k = reader.ReadInt16();
                Game.RobotStat stat = new Game.RobotStat();
                stat.Deserialize(reader);
                evt.secondaryTeamStats.Add(k, stat);
            }
            return evt;
        }
        public override string ToString()
        {
            return string.Format("{0}Game ended in {3} turns with primary losing({1}) secondary losing({2})\nPrimary Stats: {4}\nSecondaryStats: {5}", 
                base.ToString(), primaryLost, secondaryLost, turnCount, primaryTeamStats, secondaryTeamStats);
        }
    }

    public class Collision : GameEvent
    {
        internal const byte EVENT_ID = 8;
        internal List<short> collidingRobots;
        internal Vector2Int deniedPos;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(collidingRobots.GetLength());
            collidingRobots.ForEach(writer.Write);
            writer.Write(deniedPos.x);
            writer.Write(deniedPos.y);
        }
        public new static Collision Deserialize(NetworkReader reader)
        {
            Collision evt = new Collision();
            int length = reader.ReadInt32();
            evt.collidingRobots = Util.ToList(length, reader.ReadInt16);
            evt.deniedPos = new Vector2Int();
            evt.deniedPos.x = reader.ReadInt32();
            evt.deniedPos.y = reader.ReadInt32();
            return evt;
        }
        public override string ToString()
        {
            return string.Format("{0}Robots {1} collided at {2}", base.ToString(), collidingRobots, deniedPos);
        }
    }
}
