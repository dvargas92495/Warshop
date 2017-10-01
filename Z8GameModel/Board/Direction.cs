using System.Drawing;

namespace Z8GameModel
{
    internal enum Direction
    {
        North, South, East, West
    }
    internal static class DirectionMethods
    {
        // Given a Point and a Direction, this calculates what Point next to initialLocation in Direction moveInDirection
        // i.e. GetPointInDirection(Point(0,0), Direction.East) will return (1,0)
        internal static Point GetPointInDirection(Point initialLocation, Direction moveInDirection)
        {
            switch (moveInDirection)
            {
                case Direction.North:
                    return new Point(initialLocation.X, initialLocation.Y - 1);
                case Direction.South:
                    return new Point(initialLocation.X, initialLocation.Y + 1);
                case Direction.East:
                    return new Point(initialLocation.X + 1, initialLocation.Y);
                case Direction.West:
                    return new Point(initialLocation.X - 1, initialLocation.Y);

                default:
                    throw new System.Exception("Tried to move in unknown Direction: " + moveInDirection.ToString());
            }
        }
    }
}
