using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BoardController : MonoBehaviour {

    //Area board is allowed to be in
    public int boardCellsWide;
    public int boardCellsHeight;
    public TileController tile;
    public List< List<TileController>> allLocations = new List<List<TileController>>();
    public HashSet<TileController> allQueueLocations = new HashSet<TileController>();

    internal const byte BLANK_TYPE = 0;
    internal const byte QUEUE_TYPE = 1;
    internal const byte BATTERY_TYPE = 2;
    internal const byte VOID_TYPE = 3;

    // Use this for initialization
    void Awake()
    {
        Interpreter.InitializeBoard(this);
    }

    public void InitializeBoard(Map board)
    {
        boardCellsWide = board.Width;
        boardCellsHeight = board.Height;
        for (int y = 0; y<boardCellsHeight; y++)
        {
            List<TileController> row = new List<TileController>();
            for (int x = 0; x < boardCellsWide; x++)
            {
                TileController currentCell = Instantiate(tile, new Vector2(x, y), Quaternion.identity, transform);
                byte spaceType = currentCell.LoadTile(board, x, y);
                if (spaceType == QUEUE_TYPE) allQueueLocations.Add(currentCell);
                row.Add(currentCell);
            }
            allLocations.Add(row);
        }
    }
		
    public void PlaceRobot(Transform robot, int x, int y)
    {
        if (y < 0 || y >= allLocations.Count || x < 0 || x >= allLocations[y].Count)
        {
            return;
        }
        TileController loc = allLocations[y][x];
        robot.localPosition = new Vector3(loc.transform.localPosition.x, loc.transform.localPosition.y);
    }

    public TileController GetVoidTile(bool isUser)
    { //TODO - hacky
        return allLocations[isUser ? 0 : 7][2];
    }

}