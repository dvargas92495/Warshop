using System.Collections.Generic;

namespace Z8GameModel
{
    internal class Space : MapElement
    {
        private IList<int> spaceObjectIDs;
        internal bool Landable { get; set; }

        internal Space(int guid, bool landable = true) : base(guid)
        {
            Landable = landable;
            spaceObjectIDs = new List<int>();
        }

        // Called when an actor attempts to move to a space.
        // Space Objects get a chance to veto actor movement.
        // ObjectID is the object ID for the actor that is attempting to move into this space.
        internal bool AttemptedLandOn(int objectID)
        {
            bool allowedToLand = Landable;

            foreach (int oID in spaceObjectIDs)
            {
                GameObject o = Game.GetObjectFromID(oID);
                if (o.Collidable)
                {
                    allowedToLand = allowedToLand && o.Collide(objectID);
                }
            }            

            // Multiple collidable object will result in no object being able to move into a space.
            return allowedToLand;
        }

        // Called after an actor moves into a space
        internal void LandOn(int objectID)
        {
            foreach (int sOID in spaceObjectIDs)
            {
                GameObject o = Game.GetObjectFromID(sOID);
                if (o is SpaceObject) // should never fail but just in case
                {
                    ((SpaceObject)o).LandOn(objectID);
                }
            }
        }

        internal IList<int> GetSpaceObjectIDs()
        {
            return spaceObjectIDs;
        }
    }
}
