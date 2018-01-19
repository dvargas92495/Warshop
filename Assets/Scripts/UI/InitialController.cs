﻿using System.Collections.Generic;
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
    public Button myAdd;
    public Button opponentAdd;
    public Dropdown mySelect;
    public Dropdown opponentSelect;
    public Text myRoster;
    public Text opponentRoster;
    public TextAsset[] boardfiles;
    public GameObject[] robotDir;

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
                Interpreter.boardFile = t.name;
                if (myRoster.text.Length > 0)
                {
                    Interpreter.myRobotNames = myRoster.text.Substring(0, myRoster.text.Length - 1).Split('\n');
                }
                if (opponentRoster.text.Length > 0 && GameConstants.LOCAL_MODE)
                {
                    Interpreter.opponentRobotNames = opponentRoster.text.Substring(0, opponentRoster.text.Length - 1).Split('\n');
                }
                Interpreter.ConnectToServer();
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
        foreach (GameObject r in robotDir)
        {
            Dropdown.OptionData opt = new Dropdown.OptionData();
            opt.text = r.name;
            opts.Add(opt);
        }
        mySelect.AddOptions(opts);
        opponentSelect.AddOptions(opts);
    }
}
