using UnityEngine;
using WarshopCommon;

public class RobotPanelController : Controller
{
    public CommandSlotContainerController commandSlotContainer;
    public MeshRenderer powerUsedMeshRenderer;
    public SpriteRenderer robotSprite;
    public TextMesh powerUsedField;

    public void SetRobotSprite(Sprite s)
    {
        robotSprite.sprite = s;
    }

    public void SetPowerUsed(int power)
    {
        powerUsedField.text = power.ToString();
        powerUsedMeshRenderer.sortingOrder = 2;
    }

    public Sprite GetSprite()
    {
        return robotSprite.sprite;
    }

    public void ClearCommands()
    {
        commandSlotContainer.ClearCommands();
        SetPowerUsed(0);
    }

    public void AddSubmittedCommand(Command cmd, Sprite s)
    {
        int powerConsumed = commandSlotContainer.AddSubmittedCommandAndReturnPowerConsumed(cmd, s);
        SetPowerUsed(powerConsumed);
    }
}
