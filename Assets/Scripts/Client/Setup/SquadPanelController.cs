using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class SquadPanelController : MonoBehaviour
{
    public Button squadPanelButton;
    public RobotSquadImageController robotSquadImage;
    public SquadPanelRobotHolderController squadPanelRobotHolder;

    public void SetAddCallback(UnityAction<SquadPanelController> callback)
    {
        squadPanelButton.onClick.AddListener(() => callback(this));
    }

    public RobotSquadImageController AddRobotSquadImage()
    {
        return Instantiate(robotSquadImage, squadPanelRobotHolder.transform);
    }
}
