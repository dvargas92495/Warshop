﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RosterController {

    internal static SetupController initialController;

    public static void InitializeInitial(SetupController ic)
    {
        initialController = ic;
    }

    public static void maximizeRobot(string name)
    {
        initialController.maximizeSelection(name);
    }
    public static void removeFromSquad(string squadOwner, string squadName, GameObject robotName)
    {
        initialController.removeAddedFromSquad(squadOwner, squadName, robotName);
    }

}
