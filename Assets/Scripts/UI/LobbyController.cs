using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour {

    public GameObject gameSessionUI;
    public Button backButton;

	// Use this for initialization
	void Start () {
        if (GameConstants.USE_SERVER)
        {
            StartCoroutine(GameClient.SendFindAvailableGamesRequest(FindAvailableGamesCallback));
        }
        gameSessionUI.GetComponentInChildren<Button>().onClick.AddListener(NewGame);
        backButton.onClick.AddListener(() => SceneManager.LoadScene("Initial"));
    }
	
	// Update is called once per frame
	void Update () {

    }

    void FindAvailableGamesCallback(string[] gameSessionIds)
    {
        foreach (string id in gameSessionIds)
        {
            GameObject match = Instantiate(gameSessionUI, gameSessionUI.transform.parent);
            match.GetComponentInChildren<Text>().text = id.Substring(id.IndexOf("gsess-") + 6);
            match.GetComponentInChildren<Button>().onClick.AddListener(JoinGame(id));
        }
    }

    void NewGame()
    {
        foreach (Button b in gameSessionUI.transform.parent.GetComponentsInChildren<Button>()) b.interactable = false;
        if (GameConstants.USE_SERVER)
        {
            StartCoroutine(GameClient.SendCreateGameRequest(() => SceneManager.LoadScene("Setup")));
        } else
        {
            SceneManager.LoadScene("Setup");
        }
    }

    UnityAction JoinGame(string gameSessionId)
    {
        return () =>
        {
            foreach (Button b in gameSessionUI.transform.parent.GetComponentsInChildren<Button>()) b.interactable = false;
            StartCoroutine(GameClient.SendJoinGameRequest(gameSessionId,
                () => SceneManager.LoadScene("Setup"))
            );
        };
    }
}
