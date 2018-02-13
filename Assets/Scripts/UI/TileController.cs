using UnityEngine;

public class TileController : MonoBehaviour {

    public Sprite battery;
    public Sprite defaultSpace;

    public void OnMouseUp()
    {
        Interpreter.DestroyCommandMenu();
    }

    public void LoadTile(Map b, int x, int y)
    {
        Vector2Int v = new Vector2Int(x, y);
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = defaultSpace;
        if (b.IsVoid(v)) {
            sr.color = Color.black;
        } else if (b.IsQueue(v))
        {
            sr.color = Color.yellow;
        } else if (b.IsBattery(v))
        {
            sr.sprite = battery;
            sr.flipY = !b.IsPrimary(v);
        } else
        {
            sr.color = Color.white;
        }
    }

}
