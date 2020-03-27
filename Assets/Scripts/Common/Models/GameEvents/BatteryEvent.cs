using UnityEngine.Networking;

public class BatteryEvent : GameEvent
{
    internal const byte EVENT_ID = 7;
    internal short damage;
    internal bool isPrimary;

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(EVENT_ID);
        writer.Write(damage);
        writer.Write(isPrimary);
    }

    public new static BatteryEvent Deserialize(NetworkReader reader)
    {
        BatteryEvent evt = new BatteryEvent();
        evt.damage = reader.ReadInt16();
        evt.isPrimary = reader.ReadBoolean();
        return evt;
    }

    public override string ToString()
    {
        return string.Format("{0} {1} battery was damaged {2}", base.ToString(), isPrimary, damage);
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj)) return false;
        BatteryEvent other = (BatteryEvent)obj;
        return damage == other.damage && isPrimary == other.isPrimary;
    }
}
