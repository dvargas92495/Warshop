using UnityEngine;

public class SpawnCommand : RobotCommand
{

    int spawnX;
    int spawnY;

    public SpawnCommand(int x, int y)
    {
        spawnX = x;
        spawnY = y;
    }
    
    public int getSpawnX()
    {
        return spawnX;
    }

    public int getSpawnY()
    {
        return spawnY;
    }

    public override string toString()
    {
        return RobotMenuController.SPAWN + " on Space (" + spawnX + ", " + spawnY + ")";
    }
}
