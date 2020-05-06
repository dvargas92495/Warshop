using System;

namespace WarshopCommon {
    public class SpawnEvent : GameEvent
    {
        internal const byte EVENT_ID = 1;
        public SpawnEvent() {
            type = EVENT_ID;
        }

        public Tuple<int, int> destinationPos {get; set;}
        public short robotId {get; set;}

        public override string ToString()
        {
            return string.Format("{0}Robot {1} spawned at {2}", base.ToString(), robotId, destinationPos);
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj)) return false;
            SpawnEvent other = (SpawnEvent)obj;
            return destinationPos.Equals(other.destinationPos) &&
            robotId == other.robotId;
        }

        public override int GetHashCode()
        {
            return 1;
        }
    }
}
