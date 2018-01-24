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
    public Button loadBoardButton;
    public InputField myName;
    public InputField opponentName;
    public Button myAdd;
    public Button opponentAdd;
    public Dropdown mySelect;
    public Dropdown opponentSelect;
    public Text myRoster;
    public Text opponentRoster;
    public Text loadingText;
    public Toggle localModeToggle;
    public Toggle useServerToggle;
    public TextAsset playtest;
    public TextAsset[] boardfiles;
    public RobotController robotBase;
    public Sprite[] robotDir;

    public void Awake()
    {
        isServer = (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null);
    }

    // Use this for initialization
    void Start ()
    {
        App.LinkAssets(boardfiles);
        if (isServer)
        {
            GameConstants.USE_SERVER = true;
            App.StartServer();
            return;
        }
        if (playtest != null)
        {
            string[] lines = playtest.text.Split('\n');
            StartGame(
                lines[0].Trim(),
                lines[1].Trim().Split(','),
                lines[2].Trim().Split(','),
                lines[3].Trim(),
                lines[4].Trim()
            );
            return;
        }
        localModeToggle.onValueChanged.AddListener((bool val) =>
        {
            GameConstants.LOCAL_MODE = val;
            opponentName.gameObject.SetActive(val);
            opponentAdd.gameObject.SetActive(val);
            opponentSelect.gameObject.SetActive(val);
            opponentRoster.gameObject.SetActive(val);
        });
        useServerToggle.onValueChanged.AddListener((bool val) =>
        {
            GameConstants.USE_SERVER = val;
        });
        int x = -1;
        int y = 2;
        foreach (TextAsset t in boardfiles)
        {
            Button thisBoard = Instantiate(loadBoardButton, transform);
            thisBoard.GetComponentInChildren<Text>().text = t.name;
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
                StartGame(
                    t.name,
                    (myRoster.text.Length > 0 ? myRoster.text.Substring(0, myRoster.text.Length - 1).Split('\n') : new string[0]),
                    (opponentRoster.IsActive() && opponentRoster.text.Length > 0 ? opponentRoster.text.Substring(0, opponentRoster.text.Length - 1).Split('\n') : new string[0]),
                    myName.text, 
                    (opponentName.IsActive() ? opponentName.text : "")
                );
            });
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
        List<Dropdown.OptionData> opts = new List<Dropdown.OptionData>();
        foreach (Sprite r in robotDir)
        {
            Dropdown.OptionData opt = new Dropdown.OptionData();
            opt.text = r.name;
            opts.Add(opt);
        }
        mySelect.AddOptions(opts);
        opponentSelect.AddOptions(opts);
    }

    void StartGame(string b, string[] mybots, string[] opbots, string myname, string opponentname)
    {
        Interpreter.robotBase = robotBase;
        Interpreter.robotDir = robotDir;
        Interpreter.myRobotNames = mybots;
        Interpreter.opponentRobotNames = opbots;
        loadingText.text = "Loading...";
        Interpreter.ConnectToServer(myname, opponentname, b);
    }
}
