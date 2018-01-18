using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BoardController : MonoBehaviour {

    //Area board is allowed to be in
    public float boardSpaceX;
    public float boardSpaceY;
    public int boardCellsWide;
    public int boardCellsHeight;
    private float tileWidth;
    private float tileHeight;
    public GameObject tile;
    public List< List<TileController>> allLocations = new List<List<TileController>>();

    // Use this for initialization
    void Start()
    {
        Interpreter.InitializeBoard(this);
    }

    public void InitializeBoard(Map board)
    {
        boardCellsWide = board.Width;
        boardCellsHeight = board.Height;
        tileWidth = boardSpaceX / boardCellsWide;
        tileHeight = boardSpaceY / boardCellsHeight;
        float lastTileYPos = 0;
        for (int y = 0; y<boardCellsHeight; y++)
        {
            float tileXPos = (tileWidth - 1) / 2;
            float tileYPos = (tileHeight - 1) / 2;
            float lastTileXPos = 0;
            List<TileController> row = new List<TileController>();
            for (int x = 0; x < boardCellsWide; x++)
            {
                GameObject cell = Instantiate(tile, new Vector2(tileXPos + lastTileXPos, tileYPos + lastTileYPos), Quaternion.identity, transform);
                cell.transform.localScale = new Vector3(tileWidth, tileHeight, 0.1f);
                TileController currentCell = cell.GetComponent<TileController>();
                currentCell.LoadTile(board.getSpaceType(x,y));
                currentCell.BoardX = tileXPos + lastTileXPos;
                currentCell.BoardY = tileYPos + lastTileYPos;
                currentCell.RobotX = x;
                currentCell.RobotY = y;
                row.Add(currentCell);
                lastTileXPos += tileWidth;
            }
            allLocations.Add(row);
            lastTileYPos += tileHeight;
        }
    }
		
    public void PlaceRobot(Transform robot, int x, int y)
    {
        if (y < 0 || y >= allLocations.Count || x < 0 || x >= allLocations[y].Count)
        {
            return;
        }
        TileController loc = allLocations[y][x];
        robot.position = new Vector3(loc.BoardX, loc.BoardY, GameConstants.ROBOTZ);
    }

}