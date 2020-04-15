using UnityEngine;
using UnityEngine.Events;

public class BoardController : Controller
{
    public Camera cam;
    public DockController myDock;
    public DockController opponentDock;
    public RobotController robotBase;
    public TileController tile;
    public Light ceilingLight;

    private BatteryController myBattery;
    private BatteryController opponentBattery;
    private List<TileController> allLocations;

    void Awake()
    {
        BaseGameManager.InitializeBoard(this);
    }

    public void InitializeBoard(Map board)
    {
        allLocations = Util.ToList(board.spaces).Map(InitializeTile);

        InitializeLights(board.width, board.height);
    }

    public TileController InitializeTile(Map.Space s)
    {
        TileController currentCell = Instantiate(tile, new Vector2(s.x, s.y), Quaternion.identity, transform);
        currentCell.LoadTile(s, SetMyBattery, SetOpponentBattery);
        return currentCell;
    }

    public RobotController LoadRobot(Robot robot, Transform dock)
    {
        RobotController r = Instantiate(robotBase, dock);
        r.LoadModel(robot.name, robot.id);
        r.name = robot.name;
        r.displayHealth(robot.health);
        r.displayAttack(robot.attack);
        r.healthMeshRenderer.sortingOrder = 2;
        r.attackMeshRenderer.sortingOrder = 2;
        return r;
    }

    public void PlaceRobot(RobotController robot, int x, int y)
    {
        TileController loc = FindTile(x, y);
        loc.LoadRobotOnTileMesh(robot.isOpponent);
        robot.transform.localPosition = new Vector3(loc.transform.localPosition.x, loc.transform.localPosition.y, -loc.transform.localScale.z*0.1f);
    }

    public void UnplaceRobot(RobotController robot)
    {
        TileController oldLoc = FindTile(robot.transform.position.x, robot.transform.position.y);
        oldLoc.ResetMesh();
    }

    private TileController FindTile(float x, float y)
    {
        return allLocations.Find(t => t.transform.position.x == x && t.transform.position.y == y);
    }

    public void DisplayMiss(Vector2Int v, UnityAction callback)
    {
        FindTile(v.x, v.y).DisplayMiss(callback);
    }

    public BatteryController GetMyBattery()
    {
        return myBattery;
    }

    public BatteryController GetOpponentBattery()
    {
        return opponentBattery;
    }

    public TileController[] GetAllTiles()
    {
        return allLocations.ToArray();
    }

    public void SetMyBattery(BatteryController batteryController)
    {
        myBattery = batteryController;
    }

    public void SetOpponentBattery(BatteryController batteryController)
    {
        opponentBattery = batteryController;
    }

    public void SetBattery(int a, int b)
    {
        myBattery.score.text = a.ToString();
        opponentBattery.score.text = b.ToString();
    }

    public void DiffBattery(int a, int b)
    {
        myBattery.score.text = (GetMyBatteryScore() - a).ToString();
        opponentBattery.score.text = (GetOpponentBatteryScore() - b).ToString();
    }

    public int GetMyBatteryScore()
    {
        return int.Parse(myBattery.score.text);
    }

    public int GetOpponentBatteryScore()
    {
        return int.Parse(opponentBattery.score.text);
    }

    private void InitializeLights(int width, int height)
    {
        for (int y = -1; y < height + 2; y += 2)
        {
            for (int x = 1; x < width; x += 2)
            {
                Light l = Instantiate(ceilingLight, transform);
                l.transform.position += new Vector3(x - 0.5f, y);
            }
        }
    }
}