using UnityEngine;

public class TileController : MonoBehaviour {

    public Sprite battery;
    public Sprite defaultSpace;

    public void OnMouseUp()
    {
        Interpreter.DestroyCommandMenu();
    }

    public void LoadTile(Map.Space.SpaceType spaceType)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sprite = defaultSpace;
        switch (spaceType)
        {
            case Map.Space.SpaceType.VOID:
                sr.color = Color.black;
                break;
            case Map.Space.SpaceType.BLANK:
                sr.color = Color.white;
                break;
            case Map.Space.SpaceType.SPAWN:
                sr.color = Color.white;
                break;
            case Map.Space.SpaceType.PRIMARY_BASE:
                sr.sprite = battery;
                break;
            case Map.Space.SpaceType.SECONDARY_BASE:
                sr.sprite = battery;
                sr.flipY = true;
                break;
            case Map.Space.SpaceType.PRIMARY_QUEUE:
            case Map.Space.SpaceType.SECONDARY_QUEUE:
                sr.color = Color.yellow;
                break;
        }
    }

}
