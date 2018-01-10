using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class InitialController : MonoBehaviour {

    private bool isServer;
    private float dx = 200;
    private float dy = 50;
    private int width = 2;
    public Button myAdd;
    public Button opponentAdd;
    public Dropdown mySelect;
    public Dropdown opponentSelect;
    public Text myRoster;
    public Text opponentRoster;

    public void Awake()
    {
        isServer = (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null);
    }

    // Use this for initialization
    void Start () {
        if (isServer)
        {
            App.StartServer();
            return;
        }
        DirectoryInfo dir = new DirectoryInfo(GameConstants.RESOURCES + GameConstants.BOARDFILE_DIR);
        Button but = Resources.Load<Button>(GameConstants.LOADBOARD_BUTTON);
        FileInfo[] info = dir.GetFiles("*.*");
        int x = -1;
        int y = 2;
        foreach (FileInfo f in info)
        {
            if (!f.Name.EndsWith(".meta"))
            {
                Button thisBoard = Instantiate(but, transform);
                thisBoard.GetComponentInChildren<Text>().text = f.Name.Substring(0,f.Name.Length-4); // remove .txt
                RectTransform rect = thisBoard.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(x * dx, y * dy);
                x++;
                if (x >= width)
                {
                    x = -2;
                    y--;
                }
                thisBoard.onClick.AddListener(() =>
                {
                    InterpreterController.boardFile = GameConstants.BOARDFILE_DIR + "/" + f.Name.Substring(0, f.Name.Length - 4);
                    InterpreterController.myRobots = myRoster.text.Split('\n');
                    InterpreterController.opponentRobots = opponentRoster.text.Split('\n');
                    InterpreterController.ConnectToServer();
                });
            }
        }
        myAdd.onClick.AddListener(() =>
        {
            string robot = mySelect.options[mySelect.value].text;
            myRoster.text += robot + "\n";
        });
        opponentAdd.onClick.AddListener(() =>
        {
            string robot = opponentSelect.options[opponentSelect.value].text;
            opponentRoster.text += robot + "\n";
        });
        DirectoryInfo robotDir = new DirectoryInfo(GameConstants.RESOURCES + GameConstants.ROBOT_PREFAB_DIR);
        FileInfo[] robotInfo = robotDir.GetFiles("*.*");
        List<Dropdown.OptionData> opts = new List<Dropdown.OptionData>();
        foreach (FileInfo f in robotInfo)
        {
            if (!f.Name.EndsWith(".meta"))
            {
                Dropdown.OptionData opt = new Dropdown.OptionData();
                opt.text = f.Name.Substring(0, f.Name.Length - 7); //.prefab
                opts.Add(opt);
            }
        }
        mySelect.AddOptions(opts);
        opponentSelect.AddOptions(opts);
    }
}
