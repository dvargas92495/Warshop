using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class InitialController : MonoBehaviour {

    public InputField UsernameField;
    public Button EnterLobbyButton;
    public Button EnterLocalMatchButton;
    public Button ProfileButton;

    private bool isServer;
    private bool entered;
    private bool localMode;

    public void Awake()
    {
        isServer = (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null);
    }

    void Start () {
        Logger.Setup(isServer);
        if (isServer)
        {
            App.StartServer();
            return;
        }

        EnterLobbyButton.onClick.AddListener(EnterLobby);
        EnterLocalMatchButton.onClick.AddListener(EnterLocalMatch);
        ProfileButton.onClick.AddListener(EnterProfile);

        UsernameField.text = GameClient.username;
    }

    void Update()
    {
        EnterLobbyButton.interactable = !UsernameField.text.Equals("") && !entered;
    }

    void EnterLobby()
    {
        GameClient.username = UsernameField.text;

        entered = true;
        UsernameField.interactable = false;

        SceneManager.LoadScene("Lobby");
    }

    void EnterLocalMatch()
    {
        BaseGameManager.InitializeLocal();

        SceneManager.LoadScene("Setup");
    }

    void EnterProfile()
    {
        SceneManager.LoadScene("Profile");
    }
}
