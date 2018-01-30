using UnityEngine;

class Util
{
    internal static Vector2Int Flip(Vector2Int v)
    {
        return new Vector2Int(-v.x, -v.y);
    }
}
