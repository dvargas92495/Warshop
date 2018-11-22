using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class RobotRosterImageController : Controller
{
    public Button maximizeButton;
    public Image myImage;

    public void Initialize(Sprite sprite, UnityAction<Sprite> maximizeCallback)
    {
        name = sprite.name;
        myImage.sprite = sprite;
        maximizeButton.onClick.AddListener(() => maximizeCallback(sprite));
    }
}
