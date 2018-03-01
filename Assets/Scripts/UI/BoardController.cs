using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BoardController : MonoBehaviour {

    //Area board is allowed to be in
    public int boardCellsWide;
    public int boardCellsHeight;
    public TileController tile;
    public GameObject primaryDock;
    public GameObject secondaryDock;
    public GameObject Platform;
    public Light CeilingLight;
    private List< List<TileController>> allLocations = new List<List<TileController>>();
    internal HashSet<TileController> allQueueLocations = new HashSet<TileController>();
    internal TileController primaryBatteryLocation;
    internal TileController secondaryBatteryLocation;
    private TileController primaryVoidLocation;
    private TileController secondaryVoidLocation;

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
                else if (spaceType == BATTERY_TYPE && primaryBatteryLocation == null) primaryBatteryLocation = currentCell;
                else if (spaceType == BATTERY_TYPE && secondaryBatteryLocation == null) secondaryBatteryLocation = currentCell;
                else if (spaceType == VOID_TYPE && primaryVoidLocation == null) primaryVoidLocation = currentCell;
                else if (spaceType == VOID_TYPE && secondaryVoidLocation == null) secondaryVoidLocation = currentCell;
                row.Add(currentCell);
            }
            allLocations.Add(row);
            primaryDock.transform.position = new Vector3(0, -1);
            secondaryDock.transform.position = new Vector3(boardCellsWide - 1, boardCellsHeight);
        }

        for (int y = 1; y< boardCellsHeight; y+=2)
        {
            for (int x = 1; x < boardCellsWide; x+=2)
            {
                Light l = Instantiate(CeilingLight, transform);
                l.transform.position += new Vector3(x- 0.5f, y);
            }
        }
    }
		
    public void PlaceRobot(Transform robot, int x, int y)
    {
        if (y < 0 || y >= allLocations.Count || x < 0 || x >= allLocations[y].Count)
        {
            return;
        }
        TileController loc = allLocations[y][x];
        robot.localPosition = new Vector3(loc.transform.localPosition.x, loc.transform.localPosition.y, -tile.transform.localScale.z*0.501f);
    }

    public TileController GetVoidTile(bool isUser)
    {
        return isUser ? primaryVoidLocation : secondaryVoidLocation;
    }

    public Vector3 PlacePlatform(Transform t, int i)
    {
        GameObject p = Instantiate(Platform, t);
        p.transform.localPosition += Vector3.right * i;
        return p.transform.position + Vector3.back* tile.transform.localScale.z * 0.501f;
    }

}