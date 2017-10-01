using UnityEngine;

public abstract class ExecutableController : CardController {
    
    public override RobotCommand getCommand()
    {
        return new ExecutableCommand(id);
    }

    public override string getDisplayType()
    {
        return "Executable";
    }
}
