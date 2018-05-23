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

    void FindAvailableGamesCallback(string[] gameSessionIds, string[] creatorIds)
    {
        for(int i=0;i<gameSessionIds.Length; i++)
        {
            GameObject match = Instantiate(gameSessionUI, gameSessionUI.transform.parent);
            match.GetComponentsInChildren<Text>()[1].text = creatorIds[i];
            match.GetComponentInChildren<Button>().onClick.AddListener(JoinGame(gameSessionIds[i]));
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
