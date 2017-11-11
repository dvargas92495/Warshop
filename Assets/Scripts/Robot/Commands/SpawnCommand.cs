using UnityEngine;

public class SpawnCommand : RobotCommand
{

    int spawnIndex;

    public SpawnCommand(int index)
    {
        spawnIndex = index - 1;
    }
    
    public int getSpawnIndex()
    {
        return spawnIndex;
    }

    public override string toString()
    {
        return RobotMenuController.SPAWN + " on Spawn Space " + (spawnIndex + 1);
    }
}
