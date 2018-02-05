using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Rendering;

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
    public TextAsset keys;
    public Toggle localModeToggle;
    public Toggle useServerToggle;
    public TextAsset playtest;
    public TextAsset[] boardfiles;
    public RobotController robotBase;
    public Sprite[] robotDir;

    //TEMP JUST FOR PLAYTEST: DELETE
    public Text starText;
    private Dictionary<string, byte> starRatings = new Dictionary<string, byte>()
    {
        {"Bronze Grunt", 1 },
        {"Silver Grunt", 2 },
        {"Golden Grunt", 3 },
        {"Platinum Grunt", 4 },
    };

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
        if (keys != null)
        {
            string[] lines = keys.text.Split('\n');
            GameConstants.AWS_PUBLIC_KEY = lines[0].Trim();
            GameConstants.AWS_SECRET_KEY = lines[1].Trim();
        } else
        {
            GameConstants.LOCAL_MODE = true;
            GameConstants.USE_SERVER = false;
            localModeToggle.gameObject.SetActive(false);
            useServerToggle.gameObject.SetActive(false);
        }
        if (Application.isEditor && playtest != null)
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
        UnityAction<bool> opponentToggle = (bool val) =>
        {
            GameConstants.LOCAL_MODE = val;
            opponentName.gameObject.SetActive(val);
            opponentAdd.gameObject.SetActive(val);
            opponentSelect.gameObject.SetActive(val);
            opponentRoster.gameObject.SetActive(val);
        };
        UnityAction<bool> awsToggle = (bool val) =>
        {
            GameConstants.USE_SERVER = val;
        };
        if (Application.isEditor)
        {
            localModeToggle.onValueChanged.AddListener(opponentToggle);
            useServerToggle.onValueChanged.AddListener(awsToggle);
        } else
        {
            opponentToggle(false);
            awsToggle(true);
            localModeToggle.gameObject.SetActive(false);
            useServerToggle.gameObject.SetActive(false);
        }
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
                if (byte.Parse(starText.text.Substring(0, 1)) != 8) return;
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
            byte count = byte.Parse(starText.text.Substring(0,1));
            if (starRatings[robot] + count <= 8)
            {
                count += starRatings[robot];
                myRoster.text += robot + "\n";
                starText.text = count + "/8 STARS";
                Debug.Log(count);
                if (count == 8) starText.text += " READY!";
            }
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

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space)) { myRoster.text = ""; starText.text = "0/8 STARS"; }
    }

    void StartGame(string b, string[] mybots, string[] opbots, string myname, string opponentname)
    {
        RobotController.robotBase = robotBase;
        RobotController.robotDir = robotDir;
        Interpreter.myRobotNames = mybots;
        Interpreter.opponentRobotNames = opbots;
        loadingText.text = "Loading...";
        Interpreter.ConnectToServer(myname, opponentname, b);
    }
}
