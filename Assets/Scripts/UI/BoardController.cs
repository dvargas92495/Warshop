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
                currentCell.LoadTile(board, x, y);
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