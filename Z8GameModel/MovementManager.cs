using System.Collections.Generic;

namespace Z8GameModel
{
    internal static class MovementManager
    {
        // movements are only 1 space, multiple spaces is a 2.0 feature
        internal struct AttemptedMovement
        {
            internal int objectID;
            internal Direction Direction;
        }

        // This function is used to calculate how the movements in a particular phase will pan out
        // movements is all of the movements being made by the actors in a particular turn
        internal static void CalculateMovements(IList<AttemptedMovement> movements, Map map)
        {
            // A list of Guids for the edges and spaces that actors intend to move through and on
            IList<int> spaceGuids = new List<int>();

            // Attempted Movements not blocked by Attempted Pass Through
            IList<AttemptedMovement> attemptedPassSuccess = new List<AttemptedMovement>();
            // Attempted Movements not block by Attempted Land On
            IList<AttemptedMovement> attemptedLandOnSuccess = new List<AttemptedMovement>();

            // TODO: first form the chains of possible movement for each actor

            // AttemptedPassThrough and actor edge collision
            for (int i = 0; i < movements.Count; i++)
            {
                GameObject mover = Game.GetObjectFromID(movements[i].objectID);
                Edge e = map.GetEdgeFromPoint(mover.Location, movements[i].Direction);

                // First, attempted pass through
                bool canPassThrough = e.AttemptedPassThrough(mover.ObjectID);

                // Second, check if any other actors are trying to move through the same edge
                if (canPassThrough && !HandleEdgeCollision(movements, map, mover.ObjectID, e.Guid))
                {
                    attemptedPassSuccess.Add(movements[i]);
                }
            }

            // Check Pass Through, Attempted Land On, and actor space collision
            for (int i = 0; i < attemptedPassSuccess.Count; i++)
            {
                GameObject mover = Game.GetObjectFromID(movements[i].objectID);
                Edge e = map.GetEdgeFromPoint(mover.Location, movements[i].Direction);

                // Thrid do Pass Through
                e.PassThrough(attemptedPassSuccess[i].objectID);

                // Fourth do Attempted Land On
                Space s = map.GetSpaceFromPoint(DirectionMethods.GetPointInDirection(mover.Location, movements[i].Direction));
                bool canLandOn = s.AttemptedLandOn(mover.ObjectID);
            }
        }

        // A helper method for calculate movements that figures if other actors are moving through the same edge
        // Returns true if object could pass through, false if stopped
        private static bool HandleEdgeCollision(IList<AttemptedMovement> movements, Map map, int moverObjectID, int edgeGuid)
        {
            bool collided = false;
            GameObject mover = Game.GetObjectFromID(moverObjectID);
            for (int i = 0; i < movements.Count; i++)
            {
                if (movements[i].objectID != moverObjectID)
                {
                    GameObject otherMover = Game.GetObjectFromID(movements[i].objectID);
                    Edge e = map.GetEdgeFromPoint(otherMover.Location, movements[i].Direction);
                    if (e.Guid == edgeGuid && otherMover.Collidable && mover.Collidable)
                    {
                        mover.Collide(otherMover.ObjectID);
                        otherMover.Collide(mover.ObjectID);
                        collided = true;
                    }
                }
            }

            return collided;
        }

        // A helper method for calculate movements that figures if other actors are moving through the same edge
        // Returns true if object could pass through, false if stopped
        private static bool HandleSpaceCollision(IList<AttemptedMovement> movements, Map map, int moverObjectID, int spaceGuid)
        {
            bool collided = false;
            GameObject mover = Game.GetObjectFromID(moverObjectID);
            for (int i = 0; i < movements.Count; i++)
            {
                if (movements[i].objectID != moverObjectID)
                {
                    GameObject otherMover = Game.GetObjectFromID(movements[i].objectID);
                    Space s = map.GetSpaceFromPoint(DirectionMethods.GetPointInDirection(mover.Location, movements[i].Direction));
                    if (s.Guid == spaceGuid && otherMover.Collidable && mover.Collidable)
                    {
                        mover.Collide(otherMover.ObjectID);
                        otherMover.Collide(mover.ObjectID);
                        collided = true;
                    }
                }
            }

            return collided;
        }
    }
}
