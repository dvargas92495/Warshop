using UnityEngine.Networking;

public class EndEvent : GameEvent
{
    internal const byte EVENT_ID = 13;
    internal bool primaryLost;
    internal bool secondaryLost;
    internal short turnCount;

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(EVENT_ID);
        writer.Write(primaryLost);
        writer.Write(secondaryLost);
        writer.Write(turnCount);
    }

    public new static EndEvent Deserialize(NetworkReader reader)
    {
        EndEvent evt = new EndEvent();
        evt.primaryLost = reader.ReadBoolean();
        evt.secondaryLost = reader.ReadBoolean();
        evt.turnCount = reader.ReadInt16();
        return evt;
    }
    public override string ToString()
    {
        return string.Format("{0}Game ended in {3} turns with primary losing({1}) secondary losing({2})", 
            base.ToString(), primaryLost, secondaryLost, turnCount);
    }
}
