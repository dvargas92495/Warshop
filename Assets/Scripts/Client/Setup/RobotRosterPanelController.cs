using UnityEngine;
using UnityEngine.Events;

public class RobotRosterPanelController : MonoBehaviour
{
    public RobotRosterImageController robotRosterImage;

    private UnityAction<Sprite> maximizeCallback;

    public void AddRobotImage(Sprite robotImage)
    {
        RobotRosterImageController newRobotRosterImage = Instantiate(robotRosterImage, transform);
        newRobotRosterImage.Initialize(robotImage, maximizeCallback);
    }

    public void SetMaximizeCallback(UnityAction<Sprite> callback)
    {
        maximizeCallback = callback;
    }
}
