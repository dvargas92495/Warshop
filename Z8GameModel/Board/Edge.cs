using System.Collections.Generic;

namespace Z8GameModel
{
    class Edge : MapElement
    {
        internal bool Passable { get; set; }
        private IList<int> edgeObjectIDs;

        internal Edge (int guid, bool passable = true) : base(guid)
        {
            edgeObjectIDs = new List<int>();
            Passable = passable;
        }

        // Called when at an actor attempts to move acrros an edge.  
        // Edge Objects get a chance to veto actor movement as well.
        // ObjectID is a the object ID of the actor that is attempting to across the edge.
        internal bool AttemptedPassThrough(int objectID)
        {
            bool allowedToPass = Passable;

            // The actor is colliding with all of the edge objects
            foreach (int oID in edgeObjectIDs)
            {
                GameObject o = Game.GetObjectFromID(oID);
                if (o.Collidable)
                {
                    // Each collision returns a boolean as to whether is stops the actor's movement
                    allowedToPass = allowedToPass && o.Collide(objectID);
                }
            }

            return allowedToPass;
        }

        // Called after an actor moves into a space
        internal void PassThrough(int objectID)
        {
            foreach (int eOID in edgeObjectIDs)
            {
                GameObject o = Game.GetObjectFromID(eOID);
                if (o is EdgeObject) // This should never return false, but just in case
                {
                    ((EdgeObject)o).PassThrough(objectID);
                }
            }
            
        }

        internal IList<int> GetEdgeObjectIDs()
        {
            return edgeObjectIDs;
        }
    }
}
