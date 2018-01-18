using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour {

    //Model
    int points;
    bool canSpawn;
    bool isBase;
    bool isQueue;
    bool canTraverse = true;
    int AQueueCount = 0;
    int BQueueCount = 0;
    public int RobotX { get; set; }
    public int RobotY { get; set; }

    //View
    public float BoardX { get; set; }
    public float BoardY { get; set; }


    public void LoadTile(Map.Space.SpaceType spaceType)
    {
        switch (spaceType)
        {
            case Map.Space.SpaceType.VOID:
                becomeVoid();
                break;
            case Map.Space.SpaceType.BLANK:
                becomeBlank();
                break;
            case Map.Space.SpaceType.SPAWN:
                becomeSpawn();
                break;
            case Map.Space.SpaceType.BASE:
                becomeBase();
                break;
            case Map.Space.SpaceType.QUEUE:
                becomeQueue();
                break;
        }
    }

    public void SetScore(int score)
    {
        TextMesh label = gameObject.GetComponentInChildren<TextMesh>();
        label.text = score.ToString();
    }

    void becomeVoid()
    {
        canSpawn = false;
        canTraverse = false;
        points = 0;
        displayVoid();
    }

    void becomeBlank()
    {
        canSpawn = false;
        canTraverse = true;
        points = 0;
        displayBlank();
    }

    void becomeSpawn()
    {
        canSpawn = true;
        canTraverse = true;
        points = 0;
        displaySpawn();
    }

    void becomeBase()
    {
        canSpawn = false;
        canTraverse = true;
        points = 8;
        displayBase();
    }

    void becomeQueue()
    {
        isQueue = true;
        canSpawn = false;
        canTraverse = false;
        displayQueue();
    }
    void displayVoid()
    {
        gameObject.GetComponent<Renderer>().material = Resources.Load<Material>(GameConstants.TILE_MATERIAL_DIR + "Void");

    }

    void displayBlank()
    {
        gameObject.GetComponent<Renderer>().material = Resources.Load<Material>(GameConstants.TILE_MATERIAL_DIR + "White");
    }

    void displaySpawn()
    {
        gameObject.GetComponent<Renderer>().material = Resources.Load<Material>(GameConstants.TILE_MATERIAL_DIR + "Spawn");
    }

    void displayBase()
    {
        gameObject.GetComponent<Renderer>().material = Resources.Load<Material>(GameConstants.TILE_MATERIAL_DIR + "Base");
    }
    void displayQueue()
    {
        gameObject.GetComponent<Renderer>().material = Resources.Load<Material>(GameConstants.TILE_MATERIAL_DIR + "Queue");
    }	
}
