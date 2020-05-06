using System;

namespace WarshopCommon {

    public class PushEvent : GameEvent
    {
        internal const byte EVENT_ID = 5;
        public PushEvent() {
            type = EVENT_ID;
        }
        public short robotId {get; set;}
        public short victim {get; set;}
        public Tuple<int, int> direction {get; set;}

        public override string ToString()
        {
            return string.Format("{0}Robot {1} pushed {2} towards {3}", base.ToString(), robotId, victim, direction);
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj)) return false;
            PushEvent other = (PushEvent)obj;
            return other.robotId == robotId && victim == other.victim && direction.Equals(other.direction);
        }

        public override int GetHashCode()
        {
            return EVENT_ID;
        }
    }
}
