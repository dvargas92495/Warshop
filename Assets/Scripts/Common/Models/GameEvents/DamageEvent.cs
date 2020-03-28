using UnityEngine.Networking;

public class DamageEvent : GameEvent
{
    internal const byte EVENT_ID = 11;
    public short robotId;
    public short damage;
    public short remainingHealth;

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(EVENT_ID);
        writer.Write(robotId);
        writer.Write(damage);
        writer.Write(remainingHealth);
    }

    public new static DamageEvent Deserialize(NetworkReader reader)
    {
        DamageEvent evt = new DamageEvent();
        evt.robotId = reader.ReadInt16();
        evt.damage = reader.ReadInt16();
        evt.remainingHealth = reader.ReadInt16();
        return evt;
    }

    public override string ToString()
    {
        return string.Format("{0}Robot {1} took {2} damage and has {3} health remaining", base.ToString(), robotId, damage, remainingHealth);
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj)) return false;
        DamageEvent other = (DamageEvent)obj;
        return robotId == other.robotId && damage == other.damage && remainingHealth == other.remainingHealth;
    }

    public override int GetHashCode()
    {
        return EVENT_ID;
    }
}
