using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Z8GameModel
{
    // An actor may have the ability to move, attack, and rotate
    internal class Actor : SpaceObject
    {
        Direction Direction { get; set; }
        int Health { get; set; }
        int MaxHealth { get; set; }
        int Strength { get; set; } // the amount of damage dealt when attacking

        IList<int> EquipmentIDs { get; set; } // objectIDs of all equipment attached

        internal Actor(int initialx, int initialy, bool collidable, bool attackable, Direction initialDirection, int maxHealth, int strength = 0)
            : base(initialx, initialy, collidable, attackable)
        {
            Direction = initialDirection;
            MaxHealth = maxHealth;
            Strength = strength;
        }

        // Moves 1 square in a direction on a map
        // If the actor is facing the direction that it is moving in, then it has the ability to push other objects in that space
        // TODO: Figure out how to do movement & collision - does something push something else that collide into something #3
        // fail to move at all?
        internal void Move(Direction inDirection, Map map)
        {
            Location = DirectionMethods.GetPointInDirection(Location, inDirection);

            // push something if we're facing the right direction and there's something to push
            if (inDirection == Direction)
            {
                GameObject o = Game.GetObjectFromID(objectID);
                if (o.Collidable)
                {
                    o.Collide(ObjectID);
                    o.Move(inDirection, )
                }
            }
        }

        internal void Rotate(Direction newDirection)
        {
            Direction = newDirection;
        }

        // Attacks the square in front of the actor
        internal void Attack(Map map)
        {
            map.AttackFrom(Strength, ObjectID, Location, Direction);
        }
        
        internal new bool Attacked(int damage, int attackedByObjectID)
        {
            Health -= damage;
            return false;
        }

        // Called when damage is dealt from things other than attackers
        // objectID is the objectID of the source dealing damage
        internal void TakeDamage(int damage, int objectID)
        {
            Health -= damage;
        }
    }
}
