using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class InitialController : MonoBehaviour {

    public Button EnterMatchButton;
    public Button ProfileButton;

	// Use this for initialization
	void Start () {
        EnterMatchButton.onClick.AddListener(() => SceneManager.LoadScene("Setup"));
        ProfileButton.onClick.AddListener(() => SceneManager.LoadScene("Profile"));
    }
}
