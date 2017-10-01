using UnityEngine;

public class ExecutableCommand : RobotCommand
{

    int executableId;

    public ExecutableCommand(int id)
    {
        executableId = id;
    }

    public int getExecutableId()
    {
        return executableId;
    }

    public override string toString()
    {
        return RobotMenuController.EXECUTABLE + " Card " + executableId;
    }
}

