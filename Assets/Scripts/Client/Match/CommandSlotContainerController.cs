using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using WarshopCommon;

public class CommandSlotContainerController : Controller
{
    public CommandSlotController commandSlot;
    public Sprite defaultArrow;

    private List<CommandSlotController> commandSlots;

    public void Initialize(short robotId, byte robotPriority)
    {
        commandSlots = Enumerable.Range(0, GameConstants.MAX_PRIORITY).ToList().ConvertAll(c => InitializeCommand(c, robotId, robotPriority));
    }

    private CommandSlotController InitializeCommand(int c, short robotId, byte robotPriority)
    {
        CommandSlotController cmd = Instantiate(commandSlot, transform);
        cmd.Initialize(robotId, c + 1, robotPriority);
        cmd.transform.localScale = new Vector3(1, 1.0f / GameConstants.MAX_PRIORITY, 1);
        cmd.transform.localPosition = new Vector3(0, 1.0f / GameConstants.MAX_PRIORITY * (c + 1.5f) - 0.5f, 0);
        return cmd;
    }

    public void BindCommandClickCallback(RobotController r, UnityAction<RobotController, int> clickCallback)
    {
        int offset = commandSlots.FindIndex(c => c.IsNext());
        commandSlots.ForEach(c => c.BindClickCallback(defaultArrow, () => clickCallback(r, offset - commandSlots.IndexOf(c))));
    }

    public void ClearCommands()
    {
        bool clickable = true;
        commandSlots.AsEnumerable().Reverse().ToList().ForEach(child => {
            child.deletable = false;
            child.arrow.sprite = defaultArrow;
            if (!child.Closed())
            {
                child.Open();
                if (clickable) child.Next();
                clickable = false;
            }
        });
    }

    public void HighlightCommand(byte p)
    {
        CommandSlotController cmd = commandSlots[p - 1];
        if (!cmd.Closed()) cmd.Highlight();
    }

    public void ColorCommandsSubmitted()
    {
        commandSlots.ForEach(cmd =>
        {
            if (!cmd.Closed()) cmd.Submit();
        });
    }

    public int AddSubmittedCommandAndReturnPowerConsumed(Command cmd, Sprite s)
    {
        bool setNext = false;
        return commandSlots.AsEnumerable().Reverse().Aggregate(0, (powerConsumed, child) =>
        {
            if (child.Closed()) return powerConsumed;
            else if (child.Opened() && child.deletable)
            {
                byte commandId = Command.TYPES.ToList().Find(id => child.arrow.sprite.name.Equals(Command.GetDisplay(id)));
                return powerConsumed + Command.power[commandId];
            }
            else if (child.IsNext())
            {
                child.Open();
                child.arrow.sprite = s;
                child.arrow.transform.localRotation = cmd is Command.Spawn ? Quaternion.identity : Quaternion.Euler(Vector3.forward * cmd.direction * 90);
                child.deletable = true;
                return powerConsumed + Command.power[cmd.commandId];
            }
            else if (child.Opened() && !setNext && !child.deletable)
            {
                child.Next();
                setNext = true;
                return powerConsumed;
            }
            else return powerConsumed;
        });
    }

    public void DestroyCommandMenu()
    {
        commandSlots.ForEach(child => child.arrow.gameObject.SetActive(true));
    }
}
