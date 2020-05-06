namespace WarshopCommon {

    public class DeathEvent: GameEvent
    {
        internal const byte EVENT_ID = 9;
        public DeathEvent() {
            type = EVENT_ID;
        }
        public short robotId {get; set;}
        public short returnHealth {get; set;}

        public override string ToString()
        {
            return string.Format("{0}Robot {1} died and returned to {2} health", base.ToString(), robotId, returnHealth);
        }
    }
}