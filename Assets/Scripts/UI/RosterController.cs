using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RosterController {

    internal static InitialController initialController;

    public static void InitializeInitial(InitialController ic)
    {
        initialController = ic;
    }

    public static void maximizeRobot(string name)
    {
        initialController.maximizeSelection(name);
    }

    public static void addToSquad(string squadOwner, string squadName)
    {
        initialController.addSelectedToSquad(squadOwner, squadName);
    }
    public static void removeFromSquad(string squadOwner, string squadName, GameObject robotName)
    {
        initialController.removeAddedFromSquad(squadOwner, squadName, robotName);
    }

}
