using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class InitialController : MonoBehaviour {

    public InputField UsernameField;
    public Button EnterMatchButton;
    public Button ProfileButton;
    public HorizontalLayoutGroup MatchButtons;

	// Use this for initialization
	void Start () {
        EnterMatchButton.onClick.AddListener(EnterMatch);
        ProfileButton.onClick.AddListener(Profile);
    }

    void Update()
    {
        EnterMatchButton.interactable = !UsernameField.text.Equals("");
    }

    void EnterMatch()
    {
        EnterMatchButton.interactable = false;
        StartCoroutine(GameClient.SendFindAvailableGamesRequest(FindAvailableGamesCallback));
    }

    void FindAvailableGamesCallback(string[] gameSessionIds)
    {
        MatchButtons.gameObject.SetActive(true);
        foreach (string id in gameSessionIds)
        {
            Button match = Instantiate(EnterMatchButton, MatchButtons.transform);
            match.GetComponentInChildren<Text>().text = id;
            match.onClick.AddListener(JoinGame(id));
        }
        Button newGame = Instantiate(EnterMatchButton, MatchButtons.transform);
        newGame.GetComponentInChildren<Text>().text = "New Game";
        newGame.onClick.AddListener(NewGame);
        EnterMatchButton.gameObject.SetActive(false);
    }

    void NewGame()
    {
        StartCoroutine(GameClient.SendCreateGameRequest(UsernameField.text, () => SceneManager.LoadScene("Setup")));
    }

    UnityAction JoinGame(string gameSessionId)
    {
        return () =>
        {
            SceneManager.LoadScene("Setup");
        };
    }

    void Profile()
    {
        SceneManager.LoadScene("Profile");
    }
}
