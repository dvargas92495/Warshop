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

	// Use this for initialization
	void Start () {
        DirectoryInfo dir = new DirectoryInfo("Assets/Resources/" + GameConstants.BOARDFILE_DIR);
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
                    SceneManager.LoadScene("Prototype");
                });
            }
        }

    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
