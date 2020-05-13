using UnityEngine;
using UnityEngine.Events;
using TMPro;
using WarshopCommon;

public class TileController : Controller
{
    public BatteryController battery;
    public GameObject queueMarker;
    public Material baseTile;
    public Material userBaseTile;
    public Material opponentBaseTile;
    public Material opponentCore;
    public MeshRenderer meshRenderer;
    public TMP_Text spawnTileText;
    public AnimatorHelper animatorHelper;

    private Color userColor = Color.blue;
    private Color opponentColor = Color.red;
    private UnityAction<BatteryController> primaryBatterySetterCallback;
    private UnityAction<BatteryController> secondaryBatterySetterCallback;

    public void LoadTile(Map.Space s, UnityAction<BatteryController> primaryCallback, UnityAction<BatteryController> secondaryCallback)
    {
        primaryBatterySetterCallback = primaryCallback;
        secondaryBatterySetterCallback = secondaryCallback;
        if (s is Map.Battery) LoadBatteryTile((Map.Battery)s);
        else if (s is Map.Queue) LoadQueueTile((Map.Queue)s);
    }

    public void LoadBlankTile(Map.Blank s)
    {
    }

    public void LoadBatteryTile(Map.Battery s)
    {
        BatteryController newBattery = Instantiate(battery, transform.parent);
        newBattery.transform.localRotation = Quaternion.Euler(Vector3.left * 90);
        newBattery.transform.position = transform.position;

        if (s.GetIsPrimary())
        {
            primaryBatterySetterCallback(newBattery);
        }
        else
        {
            newBattery.coreRenderer.material = opponentCore;
            newBattery.score.color = Color.red;
            secondaryBatterySetterCallback(newBattery);
        }
    }

    public void LoadQueueTile(Map.Queue s)
    {
        TMP_Text spawnText = Instantiate(spawnTileText, transform);
        spawnText.text = (s.GetIndex() + 1).ToString();
        spawnText.transform.localPosition = Vector3.back * 0.101f;
        spawnText.outlineColor = s.GetIsPrimary() ? userColor : opponentColor;
    }

    public void LoadRobotOnTileMesh(bool isOpponent)
    {
        meshRenderer.material = isOpponent ? opponentBaseTile : userBaseTile;
    }

    public void ResetMesh()
    {
        meshRenderer.material = baseTile;
    }

    internal void DisplayMiss(UnityAction callback)
    {
        animatorHelper.Animate("MissAttack", callback);
    }

    public Material GetMaterial()
    {
        return meshRenderer.material;
    }

    public void SetMaterial(Material m)
    {
        meshRenderer.material = m;
    }
}
