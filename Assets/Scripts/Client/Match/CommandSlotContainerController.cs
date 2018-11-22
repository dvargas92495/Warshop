using UnityEngine;
using UnityEngine.Events;

public class CommandSlotContainerController : Controller
{
    public CommandSlotController commandSlot;
    public Sprite defaultArrow;

    private CommandSlotController[] commandSlots;

    public void Initialize(short robotId, byte robotPriority)
    {
        commandSlots = Util.Map(Util.Int(GameConstants.MAX_PRIORITY), c => InitializeCommand(c, robotId, robotPriority));
    }

    private CommandSlotController InitializeCommand(int c, short robotId, byte robotPriority)
    {
        CommandSlotController cmd = Instantiate(commandSlot, transform);
        cmd.Initialize(robotId, c, robotPriority);
        cmd.transform.localScale = new Vector3(1, 1.0f / GameConstants.MAX_PRIORITY, 1);
        cmd.transform.localPosition = new Vector3(0, 1.0f / GameConstants.MAX_PRIORITY * (c + 1.5f) - 0.5f, 0);
        return cmd;
    }

    public void BindCommandClickCallback(RobotController r, UnityAction<RobotController, int> clickCallback)
    {
        Util.ForEach(commandSlots.Length, i => commandSlots[i].BindClickCallback(defaultArrow, () => clickCallback(r, i)));
    }

    public void ClearCommands()
    {
        bool clickable = true;
        Util.ForEach(commandSlots, child => {
            child.deletable = false;
            child.Arrow.sprite = defaultArrow;
            if (!child.Closed())
            {
                child.Open();
                if (clickable) child.Next();
                clickable = false;
            }
        });
    }

    public void HighlightCommand(byte commandId, byte p)
    {
        CommandSlotController cmd = commandSlots[GameConstants.MAX_PRIORITY - p];
        if (cmd.Arrow.sprite.name.StartsWith(Command.GetDisplay(commandId)))
        {
            cmd.Highlight();
        }
    }

    public void ColorCommandsSubmitted()
    {
        Util.ForEach(commandSlots, cmd =>
        {
            if (!cmd.Closed()) cmd.Submit();
        });
    }

    public int AddSubmittedCommandAndReturnPowerConsumed(Command cmd, Sprite s)
    {
        bool setNext = false;
        return Util.Reduce(commandSlots, 0, (powerConsumed, child) =>
        {
            if (child.IsNext())
            {
                child.Open();
                child.Arrow.sprite = s;
                child.Arrow.transform.localRotation = cmd is Command.Spawn ? Quaternion.identity : Quaternion.Euler(Vector3.forward * cmd.direction * 90);
                child.deletable = true;
            }
            if (child.Closed() && !setNext)
            {
                child.Next();
                setNext = true;
                return powerConsumed;
            }
            if (child.Closed()) return powerConsumed;
            return powerConsumed + Command.power[cmd.commandId];
        });
    }

    public void DestroyCommandMenu()
    {
        Util.ForEach(commandSlots, child => child.Arrow.gameObject.SetActive(true));
    }
}
