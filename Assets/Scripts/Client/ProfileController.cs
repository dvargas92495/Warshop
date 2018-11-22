using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ProfileController : Controller
{
    public Button BackToInitialButton;

    void Start ()
    {
        BackToInitialButton.onClick.AddListener(() => SceneManager.LoadScene("Initial"));
	}
}
