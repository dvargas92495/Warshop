using UnityEngine;

public class TileController : MonoBehaviour {

    public GameObject battery;
    public Sprite[] queueSprites;
    public Sprite defaultSpace;

    public void OnMouseUp()
    {
        Interpreter.DestroyCommandMenu();
    }

    public byte LoadTile(Map b, int x, int y)
    {
        Vector2Int v = new Vector2Int(x, y);
        //sr.sprite = defaultSpace;
        if (b.IsVoid(v)) {
            //sr.color = Color.black;
            return BoardController.VOID_TYPE;
        } else if (b.IsQueue(v))
        {
            //sr.sprite = queueSprites[b.GetQueueIndex(v)];
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
