using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BoardController : MonoBehaviour {

    //Area board is allowed to be in
    public float boardSpaceX;
    public float boardSpaceY;
    public float tileWidth;
    public float tileHeight;
    public List <ProtoTileController> playerAQueueLocations = new List<ProtoTileController>();
    public List <ProtoTileController> playerBQueueLocations = new List<ProtoTileController>();
    public List<ProtoTileController> playerASpawnLocations = new List<ProtoTileController>();
    public List<ProtoTileController> playerBSpawnLocations = new List<ProtoTileController>();
    public List< List<ProtoTileController>> allLocations = new List<List<ProtoTileController>>();

    // Use this for initialization

    //Makes board using read in text file
    public void InitializeBoard(string boardfile)
    {
        TextAsset content = Resources.Load<TextAsset>(boardfile);
        string[] lines = content.text.Split('\n');
        GameObject tile = Resources.Load<GameObject>(GameConstants.PROTOTILE_PREFAB);
        int[] boardDimensions = lines[0].Trim().Split(null).Select(int.Parse).ToArray();
        int boardCellsWide = boardDimensions[0];
        int boardCellsHeight = boardDimensions[1];
        tileWidth = boardSpaceX / boardCellsWide;
        tileHeight = boardSpaceY / boardCellsHeight;
        float lastTileYPos = 0;
        for (int i = 1; i < lines.Length; i++)
        {
            string[] cells = lines[i].Trim().Split(' ');
            int y = i - 1;
            float tileXPos = (tileWidth - 1) / 2;
            float tileYPos = (tileHeight - 1) / 2;
            float lastTileXPos = 0;
            List<ProtoTileController> row = new List<ProtoTileController>();

            for (int x = 0; x < cells.Length; x++)
            {
                GameObject cell = Instantiate(tile, new Vector2(tileXPos + lastTileXPos, tileYPos + lastTileYPos), Quaternion.identity);
                cell.transform.localScale = new Vector3(tileWidth, tileHeight, 0.1f);
                cell.GetComponent<ProtoTileController>().LoadTile(cells[x]);
                ProtoTileController currentCell = cell.GetComponent<ProtoTileController>();
                currentCell.BoardX = tileXPos + lastTileXPos;
                currentCell.BoardY = tileYPos + lastTileYPos;
                currentCell.RobotX = x;
                currentCell.RobotY = y;
                if (cells[x] == "Q")
                {
                    playerAQueueLocations.Add(currentCell);
                } else if (cells[x] == "q")
                {
                    playerBQueueLocations.Add(currentCell);
                } else if (cells[x] == "S")
                {
                    playerASpawnLocations.Add(currentCell);
                    currentCell.SetScore(playerASpawnLocations.Count);
                }
                else if (cells[x] == "s")
                {
                    playerBSpawnLocations.Add(currentCell);
                    currentCell.SetScore(playerBSpawnLocations.Count);
                }
                row.Add(currentCell);
                cell.transform.SetParent(transform);
                lastTileXPos += tileWidth;
            }
            allLocations.Add(row);
            lastTileYPos += tileHeight;
        }
    }

    public void PlaceRobotInQueue(string robotIdentifer, bool isOpponent, int robotCount)
    {
        GameObject robot = GameObject.Find(robotIdentifer);
        ProtoTileController tile;
        if (isOpponent)
        {
            tile = playerBQueueLocations.ElementAt(robotCount);
        }
        else
        {
            tile = playerAQueueLocations.ElementAt(robotCount);
        }
        robot.GetComponent<RobotController>().Place(tile.RobotX, tile.RobotY);
    }
		
    public void PlaceRobot(Transform robot, int x, int y)
    {
        if (y < 0 || y >= allLocations.Count || x < 0 || x >= allLocations[y].Count)
        {
            return;
        }
        ProtoTileController loc = allLocations[y][x];
        robot.position = new Vector3(loc.BoardX, loc.BoardY, GameConstants.ROBOTZ);
    }

    public int GetNumSpawnLocations(bool isOpponent)
    {
        if (isOpponent)
        {
            return playerBSpawnLocations.Count;
        } else
        {
            return playerASpawnLocations.Count;
        }
    }

    public int[] GetSpawn(int index, bool isOpponent)
    {
        if (isOpponent)
        {
            return new int[] { playerBSpawnLocations[index].RobotX, playerBSpawnLocations[index].RobotY };
        } else
        {
            return new int[] { playerASpawnLocations[index].RobotX, playerASpawnLocations[index].RobotY };
        }
    }
}