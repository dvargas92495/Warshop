using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class InitialController : MonoBehaviour {

    public InputField UsernameField;
    public Button EnterMatchButton;
    public Button ProfileButton;

    public Toggle localModeToggle;
    public Toggle useServerToggle;

    private bool isServer;
    private bool entered;

    public void Awake()
    {
        isServer = (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null);
    }

    // Use this for initialization
    void Start () {
        Logger.Setup(isServer);
        if (isServer)
        {
            GameConstants.USE_SERVER = true;
            App.StartServer();
            return;
        }
        EnterMatchButton.onClick.AddListener(EnterMatch);
        ProfileButton.onClick.AddListener(Profile);

        GameConstants.LOCAL_MODE = Application.isEditor;
        GameConstants.USE_SERVER = !Application.isEditor;
        localModeToggle.gameObject.SetActive(Application.isEditor);
        useServerToggle.gameObject.SetActive(Application.isEditor);
        localModeToggle.onValueChanged.AddListener((bool val) => GameConstants.LOCAL_MODE = val);
        useServerToggle.onValueChanged.AddListener((bool val) => GameConstants.USE_SERVER = val);
        UsernameField.text = GameClient.username;
    }

    void Update()
    {
        EnterMatchButton.interactable = !UsernameField.text.Equals("") && !entered;
    }

    void EnterMatch()
    {
        entered = true;
        UsernameField.interactable = false;
        GameClient.username = UsernameField.text;
        SceneManager.LoadScene("Lobby");
    }

    void Profile()
    {
        SceneManager.LoadScene("Profile");
    }
}
