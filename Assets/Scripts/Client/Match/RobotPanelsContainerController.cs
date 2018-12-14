using UnityEngine;
using UnityEngine.Events;

public class RobotPanelsContainerController : Controller
{
    public RobotPanelController RobotPanel;
    public Sprite[] robotSprites;

    private Dictionary<short, RobotPanelController> robotIdToPanels;

    public void Initialize(int teamSize)
    {
        robotIdToPanels = new Dictionary<short, RobotPanelController>(teamSize);
    }

    public void AddPanel(Robot r)
    {
        RobotPanelController panel = Instantiate(RobotPanel, transform);
        panel.name += r.id;
        Sprite robotSprite = new List<Sprite>(robotSprites).Find(s => s.name.Equals(r.name));
        panel.SetRobotSprite(robotSprite);
        panel.SetPowerUsed(0);
        panel.commandSlotContainer.Initialize(r.id, r.priority);

        robotIdToPanels.Add(r.id, panel);
        int i = robotIdToPanels.GetIndex(r.id);
        panel.transform.localPosition = Vector3.right * (1.0f/robotIdToPanels.GetLength() * (i + 0.5f) - 0.5f);
    }

    public void BindCommandClickCallback(RobotController r, UnityAction<RobotController, int> clickCallback)
    {
        robotIdToPanels.Get(r.id).commandSlotContainer.BindCommandClickCallback(r, clickCallback);
    }

    public Sprite GetSprite(short robotId)
    {
        return robotIdToPanels.Get(robotId).GetSprite();
    }

    public void ClearCommands(short robotId)
    {
        robotIdToPanels.Get(robotId).ClearCommands();
    }

    public void HighlightCommands(byte commandId, byte p)
    {
        robotIdToPanels.ForEachValue(panel => panel.commandSlotContainer.HighlightCommand(commandId, p));
    }

    public void ColorCommandsSubmitted(short robotId)
    {
        robotIdToPanels.Get(robotId).commandSlotContainer.ColorCommandsSubmitted();
    }

    public void AddSubmittedCommand(Command cmd, short robotId, Sprite s)
    {
        robotIdToPanels.Get(robotId).AddSubmittedCommand(cmd, s);
    }

    public void DestroyCommandMenu()
    {
        robotIdToPanels.ForEachValue(p => p.commandSlotContainer.DestroyCommandMenu());
    }
}
