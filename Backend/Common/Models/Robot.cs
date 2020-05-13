using System;
using System.Collections.Generic;

namespace WarshopCommon {
    public class Robot
    {
        public string name {get; set;}
        public string description {get; set;}
        public byte priority {get; set;}
        public short startingHealth;
        public short health {get; set;}
        public short attack {get; set;}
        public Rating rating {get; set;}
        public short id {get; set;}
        public Tuple<int, int> position {get; set;}

        private static Logger log = new Logger(typeof(Robot).ToString());

        internal Robot(string _name, string _description)
        {
            name = _name;
            description = _description;
        }
        internal Robot(string _name, string _description, byte _priority, short _health, short _attack, Rating _rating)
        {
            name = _name;
            description = _description;
            priority = _priority;
            startingHealth = health = _health;
            attack = _attack;
            rating = _rating;
        }

        public override string ToString()
        {
            return "Robot: " + name + " - " + description + " (" + attack + "," + health + ")";
        }

        public static Robot create(string robotName)
        {
            switch(robotName)
            {
                case BronzeGrunt._name:
                    return new BronzeGrunt();
                case SilverGrunt._name:
                    return new SilverGrunt();
                case GoldenGrunt._name:
                    return new GoldenGrunt();
                case PlatinumGrunt._name:
                    return new PlatinumGrunt();
                default:
                    log.Error("Invalid Robot name: " + robotName);
                    return null;
            }
        }
        public enum Rating
        {
            PLATINUM = 4,
            GOLD = 3,
            SILVER = 2,
            BRONZE = 1
        }
        internal virtual List<Tuple<int, int>> GetVictimLocations(byte dir)
        {
            Tuple<int, int> cmd = Command.DirectionToVector(dir);
            Tuple<int, int> locs = new Tuple<int, int>(position.Item1 + cmd.Item1, position.Item2 + cmd.Item2);
            return new List<Tuple<int, int>>(){ locs };
        }

        internal virtual List<GameEvent> Spawn(Tuple<int, int> pos, bool isPrimary)
        {
            if (!position.Equals(Map.NULL_VEC)) return new List<GameEvent>();
            SpawnEvent evt = new SpawnEvent();
            evt.destinationPos = pos;
            evt.robotId = id;
            if (isPrimary) evt.primaryBatteryCost = GameConstants.DEFAULT_SPAWN_POWER;
            else evt.secondaryBatteryCost = GameConstants.DEFAULT_SPAWN_POWER;
            return new List<GameEvent>() { evt };
        }
        internal virtual List<GameEvent> Move(byte dir, bool isPrimary)
        {
            if (position.Equals(Map.NULL_VEC)) return new List<GameEvent>();
            MoveEvent evt = new MoveEvent();
            evt.sourcePos = position;
            Tuple<int, int> cmd = Command.DirectionToVector(dir);
            evt.destinationPos = new Tuple<int, int>(position.Item1 + cmd.Item1, position.Item2 + cmd.Item2);
            evt.robotId = id;
            if (isPrimary) evt.primaryBatteryCost = GameConstants.DEFAULT_MOVE_POWER;
            else evt.secondaryBatteryCost = GameConstants.DEFAULT_MOVE_POWER;
            return new List<GameEvent>() { evt };
        }
        internal virtual List<GameEvent> Attack(byte dir, bool isPrimary)
        {
            if (position.Equals(Map.NULL_VEC)) return new List<GameEvent>();
            AttackEvent evt = new AttackEvent();
            evt.locs = GetVictimLocations(dir);
            evt.robotId = id;
            if (isPrimary) evt.primaryBatteryCost = GameConstants.DEFAULT_ATTACK_POWER;
            else evt.secondaryBatteryCost = GameConstants.DEFAULT_ATTACK_POWER;
            return new List<GameEvent>(){ evt };
        }
        internal virtual short Damage(Robot victim)
        {
            return attack;
        }

        private class BronzeGrunt : Robot
        {
            internal const string _name = "Bronze Grunt";
            internal const string _description = "No Ability";
            internal BronzeGrunt() : base(
                _name,
                _description,
                5, 8, 3,
                Rating.BRONZE
            )
            { }
        }

        private class SilverGrunt : Robot
        {
            internal const string _name = "Silver Grunt";
            internal const string _description = "No Ability";
            internal SilverGrunt() : base(
                _name,
                _description,
                6, 8, 3,
                Rating.SILVER
            )
            { }
        }

        private class GoldenGrunt : Robot
        {
            internal const string _name = "Golden Grunt";
            internal const string _description = "No Ability";
            internal GoldenGrunt(): base(
                _name,
                _description,
                7, 8, 3,
                Rating.GOLD
            )
            { }
        }

        private class PlatinumGrunt : Robot
        {
            internal const string _name = "Platinum Grunt";
            internal const string _description = "No Ability";
            internal PlatinumGrunt() : base(
                _name,
                _description,
                8, 8, 3,
                Rating.PLATINUM
            )
            { }
        }
    }
}