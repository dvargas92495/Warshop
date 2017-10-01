namespace Z8GameModel
{
    internal abstract class MapElement
    {
        internal int Guid { get; set; } // a globally unique id used to check for collision

        internal MapElement(int guid)
        {
            Guid = guid;
        }
    }
}
