namespace Z8GameModel
{
    internal abstract class SpaceObject : GameObject
    {
        internal SpaceObject(int initialx, int initialy, bool collideable = false, bool attackable = false) 
            : base(initialx, initialy, collideable, attackable)
        {

        }

        internal void LandOn(int objectID)
        {

        }
    }
}
