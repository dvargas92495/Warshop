using UnityEngine;

public class AttackCommand : RobotCommand
{

    int targetX;
    int targetY;
    int damage;

    public AttackCommand() { }

    public AttackCommand(int x, int y, int d)
    {
        targetX = x;
        targetY = y;
        damage = d;
    }

    public override string toString()
    {
        return RobotMenuController.ATTACK;
    }
}
