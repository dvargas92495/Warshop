using UnityEngine;
using UnityEngine.Events;
using TMPro;


public class TileController : MonoBehaviour
{
    public BatteryController battery;
    public GameObject queueMarker;
    public Material userSpawnTileTextMaterial;
    public Material opponentSpawnTileTextMaterial;
    public Material baseTile;
    public Material userBaseTile;
    public Material opponentBaseTile;
    public Material opponentCore;
    public MeshRenderer meshRenderer;
    public TMP_Text spawnTileText;

    private UnityAction<BatteryController> primaryBatterySetterCallback;
    private UnityAction<BatteryController> secondaryBatterySetterCallback;

    public void LoadTile(Map.Space s, UnityAction<BatteryController> primaryCallback, UnityAction<BatteryController> secondaryCallback)
    {
        s.accept(this);
        primaryBatterySetterCallback = primaryCallback;
        secondaryBatterySetterCallback = secondaryCallback;
    }

    public void LoadBlankTile(Map.Blank s)
    {
    }

    public void LoadBatteryTile(Map.Battery s)
    {
        BatteryController Battery = Instantiate(battery, transform);
        Battery.transform.localRotation = Quaternion.Euler(Vector3.left * 90);
        Battery.transform.localPosition = Vector3.back * 0.5f;

        if (s.GetIsPrimary())
        {
            Battery.transform.GetChild(0).GetComponent<Renderer>().material = opponentCore;
            secondaryBatterySetterCallback(Battery);
        }
        else
        {
            primaryBatterySetterCallback(Battery);
        }
    }

    public void LoadQueueTile(Map.Queue s)
    {
        TMP_Text spawnText = Instantiate(spawnTileText, transform);
        spawnText.text = (s.GetIndex() + 1).ToString();
        spawnText.transform.localPosition = Vector3.back * 0.501f;
        spawnText.fontSharedMaterial = s.GetIsPrimary() ? userSpawnTileTextMaterial : opponentSpawnTileTextMaterial;
    }

    public void LoadRobotOnTileMesh(bool isOpponent)
    {
        meshRenderer.material = isOpponent ? opponentBaseTile : userBaseTile;
    }

    public void ResetMesh()
    {
        meshRenderer.material = baseTile;
    }
}
