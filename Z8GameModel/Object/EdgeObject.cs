namespace Z8GameModel
{
    internal abstract class EdgeObject : GameObject
    {
        internal EdgeObject(int initialx, int initialy, bool collideable = false) : base(initialx, initialy, collideable)
        {

        }

        // Whenever an object moves through an edge,
        // This function is called for each edge object
        // Occupying that edge
        // objectID: The ID of the object moving through the edge
        internal void PassThrough(int objectID)
        {

        }
    }
}
