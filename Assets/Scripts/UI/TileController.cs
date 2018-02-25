using UnityEngine;

public class TileController : MonoBehaviour {

    public Sprite battery;
    public Sprite[] queueSprites;
    public Sprite defaultSpace;

    public void OnMouseUp()
    {
        Interpreter.DestroyCommandMenu();
    }

    public byte LoadTile(Map b, int x, int y)
    {
        Vector2Int v = new Vector2Int(x, y);
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = defaultSpace;
        if (b.IsVoid(v)) {
            sr.color = Color.black;
            return BoardController.VOID_TYPE;
        } else if (b.IsQueue(v))
        {
            //sr.sprite = queueSprites[b.GetQueueIndex(v)];
            sr.color = Color.yellow;
            return BoardController.QUEUE_TYPE;
        } else if (b.IsBattery(v))
        {
            sr.sprite = battery;
            return BoardController.BATTERY_TYPE;
        } else
        {
            sr.color = Color.white;
            return BoardController.BLANK_TYPE;
        }
    }

}
