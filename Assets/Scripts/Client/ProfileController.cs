using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ProfileController : MonoBehaviour {

    public Button BackToInitialButton;

	// Use this for initialization
	void Start ()
    {
        BackToInitialButton.onClick.AddListener(() => SceneManager.LoadScene("Initial"));
	}
}
