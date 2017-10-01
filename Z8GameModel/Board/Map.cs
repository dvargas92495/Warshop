using System.Collections.Generic;
using System.Drawing;

namespace Z8GameModel
{
    internal abstract class Map
    {
        internal int Width { get; set; }
        internal int Length { get; set; }

        internal Space[,] spaces;

        Dictionary<int, Point> objectToSpace = new Dictionary<int, Point>();

        internal Map(int width, int length)
        {
            Width = width;
            Length = length;
            spaces = new Space[width, length];
        }

        // This method only updates what space is holding the object
        // It does not check that the object can move or cause any actions that would result from movement
        internal void UpdateObjectLocation(int objectID, Point NewSpace)
        {
            Point oldPoint = objectToSpace[objectID];
            spaces[oldPoint.X, oldPoint.Y].RemoveObject(objectID);

            objectToSpace[objectID] = NewSpace;
            spaces[NewSpace.X, NewSpace.Y].AddObject(objectID);
        }

        internal void RemoveObject(int objectID)
        {
            objectToSpace.Remove(objectID);
        }
    }
}
