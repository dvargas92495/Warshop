using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public abstract class GameEvent
{
    internal short primaryRobotId;
    internal byte priority;
    internal short primaryBattery;
    internal short secondaryBattery;
    public GameEvent() { }
    public void FinishMessage(NetworkWriter writer)
    {
        writer.Write(primaryRobotId);
        writer.Write(priority);
        writer.Write(primaryBattery);
        writer.Write(secondaryBattery);
    }
    public abstract void Serialize(NetworkWriter writer);
    public static GameEvent Deserialize(NetworkReader reader)
    {
        byte eventId = reader.ReadByte();
        GameEvent evt;
        switch (eventId)
        {
            case Rotate.EVENT_ID:
                evt = Rotate.Deserialize(reader);
                break;
            case Move.EVENT_ID:
                evt = Move.Deserialize(reader);
                break;
            case Attack.EVENT_ID:
                evt = Attack.Deserialize(reader);
                break;
            case Block.EVENT_ID:
                evt = Block.Deserialize(reader);
                break;
            case Push.EVENT_ID:
                evt = Push.Deserialize(reader);
                break;
            case Miss.EVENT_ID:
                evt = Miss.Deserialize(reader);
                break;
            case Battery.EVENT_ID:
                evt = Battery.Deserialize(reader);
                break;
            case Fail.EVENT_ID:
                evt = Fail.Deserialize(reader);
                break;
            case Death.EVENT_ID:
                evt = Death.Deserialize(reader);
                break;
            case Poison.EVENT_ID:
                evt = Poison.Deserialize(reader);
                break;
            case Damage.EVENT_ID:
                evt = Damage.Deserialize(reader);
                break;
            case Resolve.EVENT_ID:
                evt = Resolve.Deserialize(reader);
                break;
            default:
                evt = new Empty();
                break;
        }
        evt.primaryRobotId = reader.ReadInt16();
        evt.priority = reader.ReadByte();
        evt.primaryBattery = reader.ReadInt16();
        evt.secondaryBattery = reader.ReadInt16();
        return evt;
    }
    public override string ToString()
    {
        return "Empty Event";
    }
    public string ToString(string message)
    {
        return "Robot " + primaryRobotId + " " + message;
    }
    public void Transfer(GameEvent g)
    {
        primaryRobotId = g.primaryRobotId;
        primaryBattery = g.primaryBattery;
        secondaryBattery = g.secondaryBattery;
    }
    public void Flip()
    {
        short battery = primaryBattery;
        primaryBattery = secondaryBattery;
        secondaryBattery = battery;
    }
    public virtual void Animate(RobotController r){}
    public virtual void DisplayEvent(RobotController r) {}
    public class Empty : GameEvent
    {
        internal const byte EVENT_ID = 0;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
        }
    }

    public class Rotate : GameEvent
    {
        internal const byte EVENT_ID = 1;
        internal byte dir;
        internal Robot.Orientation sourceDir;
        internal Robot.Orientation destinationDir;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(dir);
            writer.Write((byte)sourceDir);
            writer.Write((byte)destinationDir);
        }
        public new static Rotate Deserialize(NetworkReader reader)
        {
            Rotate rot = new Rotate();
            rot.dir = reader.ReadByte();
            rot.sourceDir = (Robot.Orientation)reader.ReadByte();
            rot.destinationDir = (Robot.Orientation)reader.ReadByte();
            return rot;
        }
        public override void Animate(RobotController r)
        {
            r.displayRotate(Robot.OrientationToVector(destinationDir));
        }
        public override void DisplayEvent(RobotController r)
        {
            r.displayEvent("Rotate " + Command.Rotate.tostring[dir] + " Arrow", new Vector2Int((int)r.transform.position.x, (int)r.transform.position.y) + Robot.OrientationToVector(sourceDir));
        }
        public override string ToString()
        {
            return ToString("rotated " + Command.Rotate.tostring[dir] + " from " + sourceDir + " to " + destinationDir);
        }
    }

    public class Move : GameEvent
    {
        internal const byte EVENT_ID = 2;
        internal Vector2Int sourcePos;
        internal Vector2Int destinationPos;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(sourcePos.x);
            writer.Write(sourcePos.y);
            writer.Write(destinationPos.x);
            writer.Write(destinationPos.y);
        }
        public new static Move Deserialize(NetworkReader reader)
        {
            Move evt = new Move();
            evt.sourcePos = new Vector2Int();
            evt.sourcePos.x = reader.ReadInt32();
            evt.sourcePos.y = reader.ReadInt32();
            evt.destinationPos = new Vector2Int();
            evt.destinationPos.x = reader.ReadInt32();
            evt.destinationPos.y = reader.ReadInt32();
            return evt;
        }
        public override void Animate(RobotController r)
        {
            r.displayMove(destinationPos);
        }
        public override void DisplayEvent(RobotController r)
        {
            r.displayEvent("Move Up", destinationPos);
        }
        public override string ToString()
        {
            return ToString("moved");
        }
    }

    public class Attack : GameEvent
    {
        internal const byte EVENT_ID = 3;
        internal Vector2Int[] locs; 
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(locs.Length);
            for (int i = 0; i < locs.Length; i++)
            {
                writer.Write(locs[i].x);
                writer.Write(locs[i].y);
            }
        }
        public new static Attack Deserialize(NetworkReader reader)
        {
            Attack evt = new Attack();
            int length = reader.ReadInt32();
            evt.locs = new Vector2Int[length];
            for (int i = 0;i < length; i++)
            {
                evt.locs[i].x = reader.ReadInt32();
                evt.locs[i].y = reader.ReadInt32();
            }
            return evt;
        }
        public override void DisplayEvent(RobotController r)
        {
            Array.ForEach(locs, (Vector2Int v) =>  r.displayEvent("Attack Arrow", v));
        }
        public override string ToString()
        {
            return ToString("attacked");
        }
    }

    public class Block : GameEvent
    {
        internal const byte EVENT_ID = 4;
        internal string blockingObject;
        internal Vector2Int deniedPos;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(blockingObject);
            writer.Write(deniedPos.x);
            writer.Write(deniedPos.y);
        }
        public new static Block Deserialize(NetworkReader reader)
        {
            Block evt = new Block();
            evt.blockingObject = reader.ReadString();
            evt.deniedPos = new Vector2Int();
            evt.deniedPos.x = reader.ReadInt32();
            evt.deniedPos.y = reader.ReadInt32();
            return evt;
        }
        public override void DisplayEvent(RobotController r)
        {
            r.displayEvent("Collision", deniedPos);
        }
        public override string ToString()
        {
            return ToString("was blocked by " + blockingObject);
        }
    }

    public class Push : GameEvent
    {
        internal const byte EVENT_ID = 5;
        internal short victim;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(victim);
        }
        public new static Push Deserialize(NetworkReader reader)
        {
            Push evt = new Push();
            evt.victim = reader.ReadInt16();
            return evt;
        }
        public override string ToString()
        {
            return ToString("pushed " + victim);
        }
    }

    public class Miss : GameEvent
    {
        internal const byte EVENT_ID = 6;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
        }
        public new static Miss Deserialize(NetworkReader reader)
        {
            Miss evt = new Miss();
            return evt;
        }
        public override string ToString()
        {
            return ToString("attacked but missed");
        }
    }

    public class Battery : GameEvent
    {
        internal const byte EVENT_ID = 7;
        internal short damage;
        internal bool opponentsBase;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(damage);
            writer.Write(opponentsBase);
        }
        public new static Battery Deserialize(NetworkReader reader)
        {
            Battery evt = new Battery();
            evt.damage = reader.ReadInt16();
            evt.opponentsBase = reader.ReadBoolean();
            return evt;
        }
        public override string ToString()
        {
            return ToString("attacked " + (opponentsBase ? "opponent's":"its own") + " battery with " + damage + " damage");
        }
    }

    public class Fail : GameEvent
    {
        internal const byte EVENT_ID = 8;
        internal string failedCmd;
        internal string reason;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(failedCmd);
            writer.Write(reason);
        }
        public new static Fail Deserialize(NetworkReader reader)
        {
            Fail evt = new Fail();
            evt.failedCmd = reader.ReadString();
            evt.reason = reader.ReadString();
            return evt;
        }
        public override string ToString()
        {
            return ToString("failed to execute " + failedCmd + " beacause " + reason);
        }
    }

    public class Death: GameEvent
    {
        internal const byte EVENT_ID = 9;
        internal Vector2Int returnLocation;
        internal Robot.Orientation returnDir;
        internal short returnHealth;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(returnLocation.x);
            writer.Write(returnLocation.y);
            writer.Write((byte)returnDir);
            writer.Write(returnHealth);
        }
        public new static Death Deserialize(NetworkReader reader)
        {
            Death evt = new Death();
            evt.returnLocation = new Vector2Int();
            evt.returnLocation.x = reader.ReadInt32();
            evt.returnLocation.y = reader.ReadInt32();
            evt.returnDir = (Robot.Orientation)reader.ReadByte();
            evt.returnHealth = reader.ReadInt16();
            return evt;
        }
        public override void Animate(RobotController r)
        {
            r.displayMove(returnLocation);
            r.displayRotate(Robot.OrientationToVector(returnDir));
            r.displayHealth(returnHealth);
            r.gameObject.SetActive(false);
        }
        public override string ToString()
        {
            return ToString("dies and returns to queue");
        }
    }

    public class Poison: GameEvent
    {
        internal const byte EVENT_ID = 10;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
        }
        public new static Poison Deserialize(NetworkReader reader)
        {
            return new Poison();
        }
        public override string ToString()
        {
            return ToString("was poisoned");
        }
    }

    public class Damage : GameEvent
    {
        internal const byte EVENT_ID = 11;
        internal short damage;
        internal short remainingHealth;
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(damage);
            writer.Write(remainingHealth);
        }
        public new static Damage Deserialize(NetworkReader reader)
        {
            Damage evt = new Damage();
            evt.damage = reader.ReadInt16();
            evt.remainingHealth = reader.ReadInt16();
            return evt;
        }
        public override void Animate(RobotController r)
        {
            r.displayHealth(remainingHealth);
        }
        public override void DisplayEvent(RobotController r)
        {
            r.displayEvent("Damage", new Vector2Int((int)r.transform.position.x, (int)r.transform.position.y));
        }
        public override string ToString()
        {
            return ToString("was damaged " + damage + " health down to " + remainingHealth);
        }
    }

    public class Resolve : GameEvent
    {
        internal const byte EVENT_ID = 12;
        internal Type commandType;
        
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(EVENT_ID);
            writer.Write(GetByte(commandType));
        }
        public new static Resolve Deserialize(NetworkReader reader)
        {
            Resolve evt = new Resolve();
            evt.commandType = Command.byteToCmd[reader.ReadByte()];
            return evt;
        }
        public static byte GetByte(Type t)
        {
            return Command.byteToCmd.Keys.ToList().Find((byte b) => Command.byteToCmd[b].Equals(t));
        }
    }
}
