using UnityEngine.Networking;

public class DeathEvent: GameEvent
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
    public new static DeathEvent Deserialize(NetworkReader reader)
    {
        DeathEvent evt = new DeathEvent();
        evt.robotId = reader.ReadInt16();
        evt.returnHealth = reader.ReadInt16();
        return evt;
    }
    public override string ToString()
    {
        return string.Format("{0}Robot {1} died and returned to {2} health", base.ToString(), robotId, returnHealth);
    }
}