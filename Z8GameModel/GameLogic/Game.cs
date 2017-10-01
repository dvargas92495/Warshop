namespace Z8GameModel
{
    internal abstract class Game // all subclasses must be static
    {
        private static int guidCounter = 0;
        private static Map map { get; set; }

        internal static int GetNewGuid()
        {
            return guidCounter++;
        }

        internal static GameObject GetObjectFromID(int objectID)
        {
            return null;
        }
        
        // Kills or Removes an object
        internal static void Kill(int objectID)
        {
            map.RemoveObject(objectID);
            Game.GetObjectFromID(objectID).Kill();
        }
    }
}
