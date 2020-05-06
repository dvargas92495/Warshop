using System;

namespace WarshopCommon {
    public class MoveEvent : GameEvent
    {
        internal const byte EVENT_ID = 2;
        public MoveEvent() {
            type = EVENT_ID;
        }
        public Tuple<int, int> sourcePos {get; set;}
        public Tuple<int, int> destinationPos {get; set;}
        public short robotId {get; set;}
        
        public override string ToString()
        {
            return string.Format("{0}Robot {1} moved from {2} to {3}", base.ToString(), robotId, sourcePos, destinationPos);
        }
    }
}