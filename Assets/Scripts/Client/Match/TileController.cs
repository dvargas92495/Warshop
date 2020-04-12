using UnityEngine;
using UnityEngine.Events;
using TMPro;

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

    private Color userColor = Color.blue;
    private Color opponentColor = Color.red;
    private UnityAction<BatteryController> primaryBatterySetterCallback;
    private UnityAction<BatteryController> secondaryBatterySetterCallback;

    public void LoadTile(Map.Space s, UnityAction<BatteryController> primaryCallback, UnityAction<BatteryController> secondaryCallback)
    {
        primaryBatterySetterCallback = primaryCallback;
        secondaryBatterySetterCallback = secondaryCallback;
        s.accept(this);
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
            newBattery.transform.Rotate(0, 180, 0);
            newBattery.score.transform.Rotate(180, 180, 0);
            secondaryBatterySetterCallback(newBattery);
        }
    }

    public void LoadQueueTile(Map.Queue s)
    {
        TMP_Text spawnText = Instantiate(spawnTileText, transform);
        spawnText.text = (s.GetIndex() + 1).ToString();
        spawnText.transform.localPosition = Vector3.back * 0.501f;
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

    public Material GetMaterial()
    {
        return meshRenderer.material;
    }

    public void SetMaterial(Material m)
    {
        meshRenderer.material = m;
    }
}
