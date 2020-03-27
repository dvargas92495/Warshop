using UnityEngine.Networking;

public class ResolveEvent : GameEvent
{
    internal const byte EVENT_ID = 12;
    internal byte commandType;

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(EVENT_ID);
        writer.Write(commandType);
    }

    public new static ResolveEvent Deserialize(NetworkReader reader)
    {
        ResolveEvent evt = new ResolveEvent();
        evt.commandType = reader.ReadByte();
        return evt;
    }

    public override string ToString()
    {
        return string.Format("{0}Resolved command {1}", base.ToString(), commandType);
    }

    public override bool Equals(object obj)
    {
        if (!base.Equals(obj)) return false;
        ResolveEvent other = (ResolveEvent)obj;
        return other.commandType == commandType;
    }

    public override int GetHashCode()
    {
        return EVENT_ID;
    }
}
