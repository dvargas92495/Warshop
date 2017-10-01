using System.Drawing;
using System.Collections.Generic;

namespace Z8GameModel
{
    class Map
    {
        internal int Width { get; set; }
        internal int Length { get; set; }
        internal Space[][] Spaces { get; set; } // All of the Spaces, [x][y]

        // These data structures are just used to hold the Edges
        // To get the edge that you want, use GetEdgeFromPoint
        internal Edge[][] VerticalEdges { get; set; } // All of the Vertical Edges, [x][y]
        internal Edge[][] HorizontalEdges { get; set; } // All of the Horizontal Edges, [x][y]

        // It is the job of the child classes to fill the Spaces and the Edges
        internal Map(int width, int length)
        {
            Width = width;
            Length = length;
        }

        internal IList<int> GetSpaceObjectIDsAtPoint(Point point)
        {
            return Spaces[point.X][point.Y].GetSpaceObjectIDs();
        }

        internal IList<int> GetEdgeObjectIDsAtPoint(Point point, Direction direction)
        {
            return GetEdgeFromPoint(point, direction).GetEdgeObjectIDs();
        }

        internal Space GetSpaceFromPoint(Point point)
        {
            return Spaces[point.X][point.Y];
        }

        // Returns the Edge bordering the Space at point, in the direction of direction
        // i.e. the edge on the south side of (1,3) can be gotten with GetEdgeFromPoint(new Point(1,3), Direction.South)
        internal Edge GetEdgeFromPoint(Point point, Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return HorizontalEdges[point.X][point.Y];
                case Direction.South:
                    return HorizontalEdges[point.X][point.Y + 1];
                case Direction.East:
                    return VerticalEdges[point.X + 1][point.Y];
                case Direction.West:
                    return VerticalEdges[point.X][point.Y];
                default:
                    throw new System.Exception("Tried to move in unknown Direction: " + direction.ToString());
            }
        }

        // This method is called when an actor attacks another square
        internal void AttackFrom(int damage, int attackedByObjectID, Point point, Direction direction)
        {
            Edge e = GetEdgeFromPoint(point, direction);
            bool attackThroughEdge = true;

            foreach(int oID in e.GetEdgeObjectIDs())
            {
                GameObject o = Game.GetObjectFromID(oID);
                if (o.Attackable)
                {
                    attackThroughEdge = o.Attacked(damage, attackedByObjectID) & attackThroughEdge;
                }
            }

            if (attackThroughEdge)
            {
                Point target = DirectionMethods.GetPointInDirection(point, direction);
                Space s = Spaces[target.X][target.Y];
                foreach (int oID in s.GetSpaceObjectIDs())
                {
                    GameObject o = Game.GetObjectFromID(oID);
                    if (o.Attackable)
                    {
                        o.Attacked(damage, attackedByObjectID);
                    }
                }
            }
        }
    }
}
