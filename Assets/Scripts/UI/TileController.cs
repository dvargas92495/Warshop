using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour {

    //Model
    public int RobotX { get; set; }
    public int RobotY { get; set; }

    //View
    public float BoardX { get; set; }
    public float BoardY { get; set; }
    public Material[] tileMaterials;


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
            case Map.Space.SpaceType.PRIMARY_BASE:
            case Map.Space.SpaceType.SECONDARY_BASE:
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
        displayMaterial("Void");
    }

    void becomeBlank()
    {
        displayMaterial("White");
    }

    void becomeSpawn()
    {
        displayMaterial("Spawn");
    }

    void becomeBase()
    {
        displayMaterial("Base");
    }

    void becomeQueue()
    {
        displayMaterial("Queue");
    }

    void displayMaterial(string mat)
    {
        gameObject.GetComponent<Renderer>().material = Array.Find(tileMaterials, (Material m) => m.name.Equals(mat));
    }	
}
