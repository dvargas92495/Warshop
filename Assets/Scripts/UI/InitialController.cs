using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class InitialController : MonoBehaviour {

    public InputField UsernameField;
    public Button EnterMatchButton;
    public Button ProfileButton;
    public HorizontalLayoutGroup MatchButtons;

    public Toggle localModeToggle;
    public Toggle useServerToggle;
    public TextAsset boardfile;

    private bool isServer;
    private bool entered;

    public void Awake()
    {
        isServer = (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null);
    }

    // Use this for initialization
    void Start () {
        EnterMatchButton.onClick.AddListener(EnterMatch);
        ProfileButton.onClick.AddListener(Profile);
        Logger.Setup(isServer);
        App.LinkAssets(boardfile);
        if (isServer)
        {
            GameConstants.USE_SERVER = true;
            App.StartServer();
            return;
        }

        GameConstants.LOCAL_MODE = Application.isEditor;
        GameConstants.USE_SERVER = !Application.isEditor;
        localModeToggle.gameObject.SetActive(Application.isEditor);
        useServerToggle.gameObject.SetActive(Application.isEditor);
        localModeToggle.onValueChanged.AddListener((bool val) => GameConstants.LOCAL_MODE = val);
        useServerToggle.onValueChanged.AddListener((bool val) => GameConstants.USE_SERVER = val);
    }

    void Update()
    {
        EnterMatchButton.interactable = !UsernameField.text.Equals("") && !entered;
    }

    void EnterMatch()
    {
        entered = true;
        UsernameField.interactable = false;
        if (GameConstants.USE_SERVER)
        {
            StartCoroutine(GameClient.SendFindAvailableGamesRequest(FindAvailableGamesCallback));
        } else
        {
            GameClient.username = UsernameField.text;
            SceneManager.LoadScene("Setup");
        }
    }

    void FindAvailableGamesCallback(string[] gameSessionIds)
    {
        MatchButtons.gameObject.SetActive(true);
        foreach (string id in gameSessionIds)
        {
            Button match = Instantiate(EnterMatchButton, MatchButtons.transform);
            match.GetComponentInChildren<Text>().text = id.Substring(id.IndexOf("gsess-")+6);
            match.onClick.AddListener(JoinGame(id));
        }
        Button newGame = Instantiate(EnterMatchButton, MatchButtons.transform);
        newGame.GetComponentInChildren<Text>().text = "New Game";
        newGame.onClick.AddListener(NewGame);
        EnterMatchButton.gameObject.SetActive(false);
    }

    void NewGame()
    {
        foreach (Button b in MatchButtons.GetComponentsInChildren<Button>()) b.interactable = false;
        StartCoroutine(GameClient.SendCreateGameRequest(UsernameField.text, () => SceneManager.LoadScene("Setup")));
    }

    UnityAction JoinGame(string gameSessionId)
    {
        return () =>
        {
            foreach (Button b in MatchButtons.GetComponentsInChildren<Button>()) b.interactable = false;
            StartCoroutine(GameClient.SendJoinGameRequest(UsernameField.text, gameSessionId, 
                () => SceneManager.LoadScene("Setup"))
            );
        };
    }

    void Profile()
    {
        SceneManager.LoadScene("Profile");
    }
}
