using UnityEngine;

public class TileController : MonoBehaviour {

    public GameObject battery;
    public GameObject QueueMarker;
    public Sprite[] queueSprites;
    public Sprite defaultSpace;
    internal static Color userQueueColor = new Color(0, 0.5f, 1.0f);
    internal static Color opponentQueueColor = new Color(1.0f, 0.25f, 0);

    public byte LoadTile(Map b, int x, int y)
    {
        Vector2Int v = new Vector2Int(x, y);
        //sr.sprite = defaultSpace;
        if (b.IsVoid(v)) {
            //sr.color = Color.black;
            return BoardController.VOID_TYPE;
        } else if (b.IsQueue(v))
        {
            GameObject marker = Instantiate(QueueMarker, transform);
            marker.transform.localPosition = Vector3.back * 0.501f;
            marker.GetComponent<SpriteRenderer>().sprite = queueSprites[b.GetQueueIndex(v)];
            marker.GetComponent<SpriteRenderer>().color = b.IsPrimary(v) ? userQueueColor : opponentQueueColor;
            return BoardController.QUEUE_TYPE;
        } else if (b.IsBattery(v))
        {
            GameObject Battery = Instantiate(battery, transform);
            Battery.transform.localRotation = Quaternion.Euler(Vector3.left * 90);
            Battery.transform.localPosition = Vector3.back * 0.5f;
            Battery.transform.localScale += Vector3.up * ((1 / transform.localScale.z) - 1);
            return BoardController.BATTERY_TYPE;
        } else
        {
            //sr.color = Color.white;
            return BoardController.BLANK_TYPE;
        }
    }

}
