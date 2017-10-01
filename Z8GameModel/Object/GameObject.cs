using System.Drawing;

namespace Z8GameModel
{
    internal abstract class GameObject
    {
        internal bool Collidable { get; set; }
        internal bool Attackable { get; set; }
        internal Point Location { get; set; }
        internal int ObjectID { get; set; }

        internal GameObject(int initialx, int initialy, bool collidable = false, bool attackable = false)
        {
            Location = new Point(initialx, initialy);
            Collidable = collidable; // by default do not collide with other game objects
            Attackable = attackable; // by default object cannot be attacked
        }

        // Called when this object is moved by an actor
        internal void MovedInDirection(Direction direction)
        {
            Location = DirectionMethods.GetPointInDirection(Location, direction);
        }

        // Return whether this collision prevents the colliding actor's movement.
        // objectID is the ID of the object colliding with this object
        internal bool Collide(int objectID)
        {
            return Collidable; // By default do not stop an actor's movement
        }

        // This method is called when this object is attacked by an actor
        // It return that indicates whether the attack was stopped by the object or not
        // This is only used to determine whether an EdgeObject i.e. a wall stopped an attack 
        // before it reached all of the space objects behind it
        internal bool Attacked(int damage, int attackedByObjectID)
        {
            return false; // By default do not stop an actor's attack
        }

        // This method is called when an object is killed or removed from the game
        internal void Kill()
        {

        }
    }
}
