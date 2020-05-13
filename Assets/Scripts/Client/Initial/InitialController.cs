using UnityEngine.UI;
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

    void Start ()
    {
        enterLobbyButton.onClick.AddListener(EnterLobby);
        enterLocalMatchButton.onClick.AddListener(EnterLocalMatch);
        profileButton.onClick.AddListener(EnterProfile);

        usernameField.text = ProfileController.username;
        OnUsernameFieldEdit(usernameField.text);
        usernameField.onValueChanged.AddListener(OnUsernameFieldEdit);
        usernameField.Select();
    }

    void OnUsernameFieldEdit(string newValue)
    {
        bool valid = !string.IsNullOrEmpty(newValue);
        enterLobbyButton.interactable = valid;
        enterLocalMatchButton.interactable = valid;
        profileButton.interactable = valid;
        ProfileController.username = usernameField.text;
    }

    void EnterLobby()
    {
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
