using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class SquadPanelController : Controller
{
    public Button squadPanelButton;
    public RobotSquadImageController robotSquadImage;
    public Transform squadPanelRobotHolder;

    private List<RobotSquadImageController> squadRobots = new List<RobotSquadImageController>();

    public void SetAddCallback(UnityAction<SquadPanelController> callback)
    {
        squadPanelButton.onClick.AddListener(() => callback(this));
    }

    public RobotSquadImageController AddRobotSquadImage()
    {
        RobotSquadImageController addedRobot = Instantiate(robotSquadImage, squadPanelRobotHolder);
        squadRobots.Add(addedRobot);
        return addedRobot;
    }

    public void RemoveRobotSquadImage(RobotSquadImageController removedRobot)
    {
        squadRobots.Remove(removedRobot);
    }

    public string[] GetSquadRobotNames()
    {
        return squadRobots.Map(r => r.GetName()).ToArray();
    }

    public int GetNumRobots()
    {
        return squadRobots.GetLength();
    }
}
