namespace WarshopCommon {

    public class EndEvent : GameEvent
    {
        internal const byte EVENT_ID = 13;

        public EndEvent() {
            type = EVENT_ID;
        }
        public bool primaryLost {get; set;}
        public bool secondaryLost {get; set;}
        public short turnCount {get; set;}

        public override string ToString()
        {
            return string.Format("{0}Game ended in {3} turns with primary losing({1}) secondary losing({2})", 
                base.ToString(), primaryLost, secondaryLost, turnCount);
        }
    }

}
