using System;
using System.Collections.Generic;

namespace WarshopCommon {

    public class ResolveEvent : GameEvent
    {
        internal const byte EVENT_ID = 12;

        public ResolveEvent() {
            type = EVENT_ID;
        }
        public List<Tuple<short, Tuple<int, int>>> robotIdToSpawn {get; set;}
        public List<Tuple<short, Tuple<int, int>>> robotIdToMove {get; set;}
        public List<Tuple<short, short>> robotIdToHealth {get; set;}
        public bool myBatteryHit {get; set;}
        public bool opponentBatteryHit {get; set;}
        public List<Tuple<int, int>> missedAttacks {get;set;}
        public List<short> robotIdsBlocked {get; set;}

        public override string ToString()
        {
            return string.Format("{0}Resolved commands:\nSpawn - {1}\nMove - {2}\nHealth - {3} {4} {5} {6}\nBlocked - {7}", 
            base.ToString(), robotIdToSpawn, robotIdToMove, robotIdToHealth, myBatteryHit, opponentBatteryHit, missedAttacks, robotIdsBlocked);
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj)) return false;
            ResolveEvent other = (ResolveEvent)obj;
            return other.robotIdToSpawn.Equals(robotIdToSpawn) 
            && other.robotIdToMove.Equals(robotIdToMove) 
            && other.robotIdToHealth.Equals(robotIdToHealth) 
            && other.myBatteryHit.Equals(myBatteryHit) 
            && other.opponentBatteryHit.Equals(opponentBatteryHit) 
            && other.missedAttacks.Equals(missedAttacks) 
            && other.robotIdsBlocked.Equals(robotIdsBlocked);
        }

        public override int GetHashCode()
        {
            return EVENT_ID;
        }

        public int GetNumResolutions()
        {
            return robotIdToSpawn.Count 
            + robotIdToMove.Count 
            + robotIdToHealth.Count
            + (myBatteryHit ? 1 : 0)
            + (opponentBatteryHit ? 1 : 0)
            + missedAttacks.Count
            + robotIdsBlocked.Count;
        }
    }
}