﻿using UnityEngine;
using TMPro;


public class TileController : MonoBehaviour {

    public GameObject battery;
    public GameObject QueueMarker;
    public Sprite[] queueSprites;
    public TMP_Text spawnTileText;
    public Material userSpawnTileTextMaterial;
    public Material opponentSpawnTileTextMaterial;
    public Material BaseTile;
    public Material UserBaseTile;
    public Material OpponentBaseTile;
    public Sprite defaultSpace;
    //Not used for now:
    //internal static Color userQueueColor = new Color(0, 0.5f, 1.0f);
    //internal static Color opponentQueueColor = new Color(1.0f, 0.25f, 0);

    public byte LoadTile(Map b, int x, int y)
    {
        Vector2Int v = new Vector2Int(x, y);
        //sr.sprite = defaultSpace;
        if (b.IsVoid(v)) {
            //sr.color = Color.black;
            return BoardController.VOID_TYPE;
        } else if (b.IsQueue(v))
        {
            TMP_Text spawnText = Instantiate(spawnTileText, transform);
            spawnText.text = (b.GetQueueIndex(v)+1).ToString();
            spawnText.transform.localPosition = Vector3.back * 0.501f;
            spawnText.fontSharedMaterial = b.IsPrimary(v) ? userSpawnTileTextMaterial : opponentSpawnTileTextMaterial;

            return BoardController.QUEUE_TYPE;
        } else if (b.IsBattery(v))
        {
            GameObject Battery = Instantiate(battery, transform);
            Battery.transform.localRotation = Quaternion.Euler(Vector3.left * 90);
            Battery.transform.localPosition = Vector3.back * 0.5f;
           //Battery.transform.localScale += Vector3.up * ((1 / transform.localScale.z) - 1);
            return BoardController.BATTERY_TYPE;
        }
        

        else
        {
            //sr.color = Color.white;
            return BoardController.BLANK_TYPE;
        }
    }

}
