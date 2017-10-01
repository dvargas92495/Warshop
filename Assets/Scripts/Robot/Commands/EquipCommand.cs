using UnityEngine;

public class EquipCommand : RobotCommand
{

    int equipmentId;

    public EquipCommand(int id)
    {
        equipmentId = id;
    }

    public int getEquipmentId()
    {
        return equipmentId;
    }

    public override string toString()
    {
        return RobotMenuController.EQUIP + " Card " + equipmentId; //TODO: Change
    }
}

