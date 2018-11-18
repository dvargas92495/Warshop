using UnityEngine;
using UnityEngine.Events;

public class CommandSlotController : MonoBehaviour {

    public SpriteRenderer Arrow;
    public SpriteRenderer Delete;
    internal bool deletable;

    private UnityAction myClick;
    private static Color NO_COMMAND = new Color(0.25f, 0.25f, 0.25f);
    private static Color HIGHLIGHTED_COMMAND = new Color(0.5f, 0.5f, 0.5f);
    private static Color SUBMITTED_COMMAND = new Color(0.75f, 0.75f, 0.75f);
    private static Color NEXT_COMMAND = new Color(0.5f, 1, 0.5f);
    private static Color OPEN_COMMAND = new Color(1, 1, 1);

    void OnMouseEnter()
    {
        Delete.gameObject.SetActive(deletable);
    }

    void OnMouseExit()
    {
        Delete.gameObject.SetActive(false);
    }

    void OnMouseUp()
    {
        myClick();
    }

    internal void Initialize(short rid, int i, byte p)
    {
        if (i > p)
        {
            Arrow.color = NO_COMMAND;
        }else if (i == p)
        {
            Arrow.color = NEXT_COMMAND;
        }
        deletable = false;
    }

    internal void BindClickCallback(Sprite defaultArrow, UnityAction clickCallback)
    {
        myClick = () => {
            if (deletable)
            {
                clickCallback();
                deletable = !Arrow.sprite.Equals(defaultArrow);
                Delete.gameObject.SetActive(deletable);
            }
        };
    }

    internal void Open()
    {
        Arrow.color = OPEN_COMMAND;
        Arrow.transform.localRotation = Quaternion.identity;
    }

    internal bool Opened()
    {
        return Arrow.color.Equals(OPEN_COMMAND);
    }

    internal bool Closed()
    {
        return Arrow.color.Equals(NO_COMMAND);
    }

    internal void Highlight()
    {
        Arrow.color = HIGHLIGHTED_COMMAND;
    }

    internal bool Highlighted()
    {
        return Arrow.color.Equals(HIGHLIGHTED_COMMAND);
    }

    internal void Submit()
    {
        Arrow.color = SUBMITTED_COMMAND;
        deletable = false;
    }

    internal void Next()
    {
        Arrow.color = NEXT_COMMAND;
    }

    internal bool IsNext()
    {
        return Arrow.color.Equals(NEXT_COMMAND);
    }
}
