using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using WarshopCommon;

public class RobotPanelsContainerController : Controller
{
    public RobotPanelController RobotPanel;
    public Sprite[] robotSprites;

    private Dictionary<short, RobotPanelController> robotIdToPanels;

    public void Initialize(int teamSize)
    {
        robotIdToPanels = new Dictionary<short, RobotPanelController>(teamSize);
    }

    public bool Contains(short id)
    {
        return robotIdToPanels.ContainsKey(id);
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
        List<short> robotIds = robotIdToPanels.Keys.ToList();
        robotIds.Sort();
        int i = robotIds.IndexOf(r.id);
        panel.transform.localPosition = Vector3.right * (1.0f/robotIdToPanels.Count * (i + 0.5f) - 0.5f);
    }

    public void BindCommandClickCallback(RobotController r, UnityAction<RobotController, int> clickCallback)
    {
        robotIdToPanels[r.id].commandSlotContainer.BindCommandClickCallback(r, clickCallback);
    }

    public Sprite GetSprite(short robotId)
    {
        return robotIdToPanels[robotId].GetSprite();
    }

    public void ClearCommands(short robotId)
    {
        robotIdToPanels[robotId].ClearCommands();
    }

    public void HighlightCommands(byte p)
    {
        robotIdToPanels.Values.ToList().ForEach(panel => panel.commandSlotContainer.HighlightCommand(p));
    }

    public void ColorCommandsSubmitted(short robotId)
    {
        robotIdToPanels[robotId].commandSlotContainer.ColorCommandsSubmitted();
    }

    public void AddSubmittedCommand(Command cmd, short robotId, Sprite s)
    {
        robotIdToPanels[robotId].AddSubmittedCommand(cmd, s);
    }

    public void DestroyCommandMenu()
    {
        robotIdToPanels.Values.ToList().ForEach(p => p.commandSlotContainer.DestroyCommandMenu());
    }
}
