using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BoardController : MonoBehaviour {

    //Area board is allowed to be in
    public TextAsset DefaultBoard;
    public int boardCellsWide;
    public int boardCellsHeight;
    public TileController tile;
    public GameObject primaryDock;
    public GameObject secondaryDock;
    public Material OpponentQueueBeamMaterial;
    public RobotController robotBase;
    public GameObject[] RobotModels;
    public Light CeilingLight;
    public Camera cam;
    private List< List<TileController>> allLocations = new List<List<TileController>>();
    internal HashSet<TileController> allQueueLocations = new HashSet<TileController>();
    internal TileController primaryBatteryLocation;
    internal TileController secondaryBatteryLocation;
    private TileController primaryVoidLocation;
    private TileController secondaryVoidLocation;
    private bool[] primaryDockOccupied = new bool[] { false, false, false, false};
    private bool[] secondaryDockOccupied = new bool[] { false, false, false, false };

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

        for (int y = -1; y< boardCellsHeight+2; y+=2)
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
        if (y < 0 || y >= boardCellsHeight || x < 0 || x >= boardCellsWide)
        {
            return;
        }
        UnplaceRobot(robot);
        TileController loc = allLocations[y][x];
        loc.GetComponent<MeshRenderer>().material = robot.GetComponent<RobotController>().isOpponent ? tile.OpponentBaseTile : tile.UserBaseTile;
        robot.localPosition = new Vector3(loc.transform.localPosition.x, loc.transform.localPosition.y, -tile.transform.localScale.z*0.501f);
    }

    public void UnplaceRobot(Transform robot)
    {
        int oldy = (int)robot.position.y;
        int oldx = (int)robot.position.x;
        foreach (RobotController other in Interpreter.robotControllers.Values)
        {
            if (other.transform.position.x == oldx && other.transform.position.y== oldy && !other.transform.Equals(robot))
            {
                return;
            }
        }
        if (oldy >= 0 && oldy < boardCellsHeight && oldx >= 0 && oldx < boardCellsWide)
        {
            TileController oldLoc = allLocations[oldy][oldx];
            oldLoc.GetComponent<MeshRenderer>().material = tile.BaseTile;
        }
    }

    public TileController GetVoidTile(bool isUser)
    {
        return isUser ? primaryVoidLocation : secondaryVoidLocation;
    }

    public Vector3 PlaceInBelt(bool isPrimary)
    {
        int i;
        bool[] isOccupied = (isPrimary ? primaryDockOccupied : secondaryDockOccupied);
        for (i = 0;i<isOccupied.Length; i++)
        {
            if (!isOccupied[i])
            {
                isOccupied[i] = true;
                break;
            }
        }
        return Vector3.right * i + Vector3.back * tile.transform.localScale.z * 1.001f;
    }

    public void RemoveFromBelt(Vector3 localPos, bool isPrimary)
    {
        int i = (int)localPos.x;
        bool[] isOccupied = (isPrimary ? primaryDockOccupied : secondaryDockOccupied);
        isOccupied[i] = false;
    }

    public void ColorQueueBelt(bool isPrimary)
    {
        Transform t = isPrimary ? secondaryDock.transform : primaryDock.transform;
        for (int i = 0; i < t.GetChild(0).childCount; i++)
        {
            if (t.GetChild(0).GetChild(i).name.StartsWith("Cylinder"))
            {
                MeshRenderer m = t.GetChild(0).GetChild(i).GetComponent<MeshRenderer>();
                m.material = OpponentQueueBeamMaterial;
            }
        }
    }
}