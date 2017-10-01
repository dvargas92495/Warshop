using UnityEngine;

public abstract class EquipController : CardController {

    public override RobotCommand getCommand()
    {
        return new EquipCommand(id);
    }

    public override string getDisplayType()
    {
        return "Equipment";
    }
}
