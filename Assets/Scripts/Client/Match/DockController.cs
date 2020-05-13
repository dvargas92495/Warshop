using UnityEngine;
using WarshopCommon;

public class DockController : Controller
{
    private bool[] isOccupied = new bool[GameConstants.MAX_ROBOTS_ON_SQUAD];

    public Vector3 PlaceInBelt()
    {
        int i;
        for (i = 0; i < isOccupied.Length; i++)
        {
            if (!isOccupied[i])
            {
                isOccupied[i] = true;
                break;
            }
        }
        return Vector3.right * i + Vector3.back * .2f;
    }

    public void RemoveFromBelt(Vector3 localPos)
    {
        int i = (int)localPos.x;
        isOccupied[i] = false;
    }
}
