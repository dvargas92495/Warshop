using UnityEngine;
using UnityEngine.Events;

public class CommandSlotController : Controller
{
    public SpriteRenderer arrow;
    public SpriteRenderer delete;
    internal bool deletable;

    private UnityAction myClick;
    private static Color NO_COMMAND = new Color(0.25f, 0.25f, 0.25f);
    private static Color HIGHLIGHTED_COMMAND = new Color(0.5f, 0.5f, 0.5f);
    private static Color SUBMITTED_COMMAND = new Color(0.75f, 0.75f, 0.75f);
    private static Color NEXT_COMMAND = new Color(0.5f, 1, 0.5f);
    private static Color OPEN_COMMAND = new Color(1, 1, 1);

    void OnMouseEnter()
    {
        delete.gameObject.SetActive(deletable);
    }

    void OnMouseExit()
    {
        delete.gameObject.SetActive(false);
    }

    void OnMouseUp()
    {
        myClick();
    }

    internal void Initialize(short rid, int i, byte p)
    {
        if (i > p)
        {
            arrow.color = NO_COMMAND;
        }else if (i == p)
        {
            arrow.color = NEXT_COMMAND;
        }
        deletable = false;
    }

    internal void BindClickCallback(Sprite defaultArrow, UnityAction clickCallback)
    {
        myClick = () => {
            if (deletable)
            {
                clickCallback();
                deletable = !arrow.sprite.Equals(defaultArrow);
                delete.gameObject.SetActive(deletable);
            }
        };
    }

    internal void Open()
    {
        arrow.color = OPEN_COMMAND;
        arrow.transform.localRotation = Quaternion.identity;
    }

    internal bool Opened()
    {
        return arrow.color.Equals(OPEN_COMMAND);
    }

    internal bool Closed()
    {
        return arrow.color.Equals(NO_COMMAND);
    }

    internal void Highlight()
    {
        arrow.color = HIGHLIGHTED_COMMAND;
    }

    internal bool Highlighted()
    {
        return arrow.color.Equals(HIGHLIGHTED_COMMAND);
    }

    internal void Submit()
    {
        arrow.color = SUBMITTED_COMMAND;
        deletable = false;
    }

    internal void Next()
    {
        arrow.color = NEXT_COMMAND;
    }

    internal bool IsNext()
    {
        return arrow.color.Equals(NEXT_COMMAND);
    }
}
