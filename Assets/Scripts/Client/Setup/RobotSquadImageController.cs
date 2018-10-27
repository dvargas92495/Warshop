using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class RobotSquadImageController : MonoBehaviour
{
    public Button removeButton;
    public Image robotImage;

    private byte rating;

    public void SetRemoveCallback(UnityAction<RobotSquadImageController> callback)
    {
        removeButton.onClick.AddListener(() => callback(this));
    }

    public void SetSprite(Sprite robotSprite)
    {
        robotImage.sprite = robotSprite;
    }

    public void SetRating(byte r)
    {
        rating = r;
    }

    public byte GetRating()
    {
        return rating;
    }
}
