using System;
using System.Text.Json;

namespace WarshopCommon {
    public abstract class GameEvent
    {
        public byte priority {get; set; }
        public short primaryBatteryCost {get; set;}
        public short secondaryBatteryCost {get; set;}
        public byte type { get; set; }

        public string Serialize() {
            return JsonSerializer.Serialize(this);
        }
        public static GameEvent Deserialize(string msg)
        {
            byte eventId = JsonSerializer.Deserialize<GameEvent>(msg).type;
            GameEvent evt;
            switch (eventId)
            {
                case SpawnEvent.EVENT_ID:
                    evt = JsonSerializer.Deserialize<SpawnEvent>(msg);
                    break;
                case MoveEvent.EVENT_ID:
                    evt = JsonSerializer.Deserialize<MoveEvent>(msg);
                    break;
                case AttackEvent.EVENT_ID:
                    evt = JsonSerializer.Deserialize<AttackEvent>(msg);
                    break;
                case PushEvent.EVENT_ID:
                    evt = JsonSerializer.Deserialize<PushEvent>(msg);
                    break;
                case DeathEvent.EVENT_ID:
                    evt = JsonSerializer.Deserialize<DeathEvent>(msg);
                    break;
                case ResolveEvent.EVENT_ID:
                    evt = JsonSerializer.Deserialize<ResolveEvent>(msg);
                    break;
                case EndEvent.EVENT_ID:
                    evt = JsonSerializer.Deserialize<EndEvent>(msg);
                    break;
                default:
                    throw new Exception("Unknown Event Id to deserialize: " + eventId);
            }
            return evt;
        }
        public override string ToString()
        {
            return string.Format("Event at {0} costing ({1},{2}) - ", priority, primaryBatteryCost, secondaryBatteryCost);
        }
        public override bool Equals(object obj)
        {
            if (!GetType().Equals(obj.GetType())) return false;
            GameEvent other = (GameEvent)obj;
            return priority == other.priority &&
                primaryBatteryCost == other.primaryBatteryCost &&
                secondaryBatteryCost == other.secondaryBatteryCost;
        }
        public void Flip()
        {
            short battery = primaryBatteryCost;
            primaryBatteryCost = secondaryBatteryCost;
            secondaryBatteryCost = battery;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(priority, primaryBatteryCost, secondaryBatteryCost, type);
        }
    }
}
