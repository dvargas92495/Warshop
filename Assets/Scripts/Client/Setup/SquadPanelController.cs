using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class SquadPanelController : MonoBehaviour
{
    public Button squadPanelButton;
    public RobotSquadImageController robotSquadImage;
    public Transform squadPanelRobotHolder;

    private RobotSquadImageController[] squadRobots = new RobotSquadImageController[0];

    public void SetAddCallback(UnityAction<SquadPanelController> callback)
    {
        squadPanelButton.onClick.AddListener(() => callback(this));
    }

    public RobotSquadImageController AddRobotSquadImage()
    {
        RobotSquadImageController addedRobot = Instantiate(robotSquadImage, squadPanelRobotHolder);
        squadRobots = Util.Add(squadRobots, addedRobot);
        return addedRobot;
    }

    public void RemoveRobotSquadImage(RobotSquadImageController removedRobot)
    {
        squadRobots = Util.Remove(squadRobots, removedRobot);
    }

    public string[] GetSquadRobotNames()
    {
        return Util.Map(squadRobots, (RobotSquadImageController r) => r.name.Trim());
    }

    public int GetNumRobots()
    {
        return squadRobots.Length;
    }
}
