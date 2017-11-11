using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class InitialController : MonoBehaviour {

    private float dx = 200;
    private float dy = 50;
    private int width = 2;
    public Button playerAAdd;
    public Button playerBAdd;
    public Dropdown playerASelect;
    public Dropdown playerBSelect;
    public Text playerARoster;
    public Text playerBRoster;

    // Use this for initialization
    void Start () {
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
                    InterpreterController.playerARobots = playerARoster.text.Split('\n');
                    InterpreterController.playerBRobots = playerBRoster.text.Split('\n');
                    SceneManager.LoadScene("Prototype");
                });
            }
        }
        playerAAdd.onClick.AddListener(() =>
        {
            string robot = playerASelect.options[playerASelect.value].text;
            playerARoster.text += robot + "\n";
        });
        playerBAdd.onClick.AddListener(() =>
        {
            string robot = playerBSelect.options[playerBSelect.value].text;
            playerBRoster.text += robot + "\n";
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
        playerASelect.AddOptions(opts);
        playerBSelect.AddOptions(opts);
    }

    // Update is called once per frame
    void Update () {
		
	}
}
