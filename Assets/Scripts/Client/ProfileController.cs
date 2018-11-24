using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ProfileController : Controller
{
    public Button backToInitialButton;
    public SceneReference initialScene;

    void Start ()
    {
        backToInitialButton.onClick.AddListener(BackToInitial);
	}

    void BackToInitial()
    {
        SceneManager.LoadScene(initialScene);
    }
}
