using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotMenuController : MonoBehaviour {

    MenuOptions myOption;
    RobotController parentBot;
    string value;
    public const string ATTACK = "Attack";
    public const string ROTATE = "Rotate";
    public const string MOVE = "Move";
    public const string SPAWN = "Spawn";
    public const string EQUIP = "Equip";
    public const string EXECUTABLE = "Execute";
    public static Dictionary<MenuOptions, string> LABELS = new Dictionary<MenuOptions, string>
    {
        { MenuOptions.ATTACK, ATTACK },
        { MenuOptions.MOVE, MOVE },
        { MenuOptions.ROTATE, ROTATE },
        { MenuOptions.SPAWN, SPAWN },
        { MenuOptions.EQUIP, EQUIP },
        { MenuOptions.EXECUTABLE, EXECUTABLE },
    };

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnMouseUp()
    {
        parentBot.ClickMenuOption(myOption,value);
    }

    public void SetOptionRobotValue(MenuOptions opt, RobotController robot, string val)
    {
        myOption = opt;
        parentBot = robot;
        value = val;
    }

    public MenuOptions GetOption()
    {
        return myOption;
    }

    public string GetValue()
    {
        return value;
    }
}
