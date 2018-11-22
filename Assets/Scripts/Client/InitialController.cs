using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class InitialController : Controller
{
    public Button enterLobbyButton;
    public Button enterLocalMatchButton;
    public Button profileButton;
    public InputField usernameField;

    private bool isServer;
    private bool localMode;
    private Scene lobbyScene;
    private Scene profileScene;
    private Scene setupScene;

    void Awake()
    {
        isServer = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
    }

    void Start () {
        Logger.Setup(isServer);
        if (isServer)
        {
            App.StartServer();
            return;
        }

        enterLobbyButton.onClick.AddListener(EnterLobby);
        enterLocalMatchButton.onClick.AddListener(EnterLocalMatch);
        profileButton.onClick.AddListener(EnterProfile);

        usernameField.text = GameClient.username;
        usernameField.onValueChanged.AddListener(v => enterLobbyButton.interactable = v.Equals(""));
    }

    void EnterLobby()
    {
        GameClient.username = usernameField.text;

        enterLobbyButton.interactable = false;
        usernameField.interactable = false;

        SceneManager.LoadScene(lobbyScene.name);
    }

    void EnterLocalMatch()
    {
        BaseGameManager.InitializeLocal();

        SceneManager.LoadScene(setupScene.name);
    }

    void EnterProfile()
    {
        SceneManager.LoadScene(profileScene.name);
    }
}
