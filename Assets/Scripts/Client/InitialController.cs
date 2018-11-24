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
    public SceneReference lobbyScene;
    public SceneReference profileScene;
    public SceneReference setupScene;

    private bool isServer;
    private bool localMode;

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
        Debug.Log(setupScene.ScenePath);

        enterLobbyButton.onClick.AddListener(EnterLobby);
        enterLocalMatchButton.onClick.AddListener(EnterLocalMatch);
        profileButton.onClick.AddListener(EnterProfile);

        usernameField.text = GameClient.username;
        usernameField.onValueChanged.AddListener(OnUsernameFieldEdit);
    }

    void OnUsernameFieldEdit(string newValue)
    {
        bool valid = !string.IsNullOrEmpty(newValue);
        enterLobbyButton.interactable = valid;
        enterLocalMatchButton.interactable = valid;
        profileButton.interactable = valid;
    }

    void EnterLobby()
    {
        GameClient.username = usernameField.text;

        enterLobbyButton.interactable = false;
        usernameField.interactable = false;

        SceneManager.LoadScene(lobbyScene);
    }

    void EnterLocalMatch()
    {
        BaseGameManager.InitializeLocal();

        SceneManager.LoadScene(setupScene);
    }

    void EnterProfile()
    {
        SceneManager.LoadScene(profileScene);
    }
}
