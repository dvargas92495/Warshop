using System;
using System.Collections.Generic;

namespace WarshopCommon {

    public class AttackEvent : GameEvent
    {
        internal const byte EVENT_ID = 3;

        public AttackEvent() {
            type = EVENT_ID;
        }
        public List<Tuple<int, int>> locs {get; set;}
        public short robotId {get; set;}

        public override string ToString()
        {
            return string.Format("{0}Robot {1} attacked {2}", base.ToString(), robotId, locs);
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj)) return false;
            AttackEvent other = (AttackEvent)obj;
            return robotId == other.robotId && locs.Equals(other.locs);
        }

        public override int GetHashCode()
        {
            return EVENT_ID;
        }
    }
}
