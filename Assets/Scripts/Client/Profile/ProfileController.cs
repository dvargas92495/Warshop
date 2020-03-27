using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ProfileController : Controller
{
    public Button backToInitialButton;
    public Text usernameText;
    public SceneReference initialScene;

    internal static string username;

    void Start ()
    {
        usernameText.text = username;
        backToInitialButton.onClick.AddListener(BackToInitial);
	}

    void BackToInitial()
    {
        SceneManager.LoadScene(initialScene);
    }
}
