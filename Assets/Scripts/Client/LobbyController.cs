using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour {

    public GameObject newgameSessionUI;
    public GameObject gameSessionUI;
    public Button backButton;
    public Text statusText;

    void Start () {
        if (GameConstants.USE_SERVER)
        {
            StartCoroutine(GameClient.SendFindAvailableGamesRequest(FindAvailableGamesCallback));
        }
        newgameSessionUI.GetComponentInChildren<Button>().onClick.AddListener(NewGame);
        backButton.onClick.AddListener(() => SceneManager.LoadScene("Initial"));
    }

    void Update()
    {
        bool isPrivate = newgameSessionUI.GetComponentInChildren<Dropdown>().value == 1;
        InputField passwordField = newgameSessionUI.GetComponentInChildren<InputField>(true);
        passwordField.gameObject.SetActive(isPrivate);
        newgameSessionUI.GetComponentInChildren<Button>().interactable = !isPrivate || !passwordField.text.Equals("");
        statusText.transform.parent.gameObject.SetActive(!BaseGameManager.ErrorString.Equals(""));
        statusText.text = BaseGameManager.ErrorString;
        if (Input.GetMouseButtonUp(0) && !BaseGameManager.ErrorString.Equals(""))
        {
            BaseGameManager.ErrorString = "";
            foreach (Button b in newgameSessionUI.transform.parent.GetComponentsInChildren<Button>()) b.interactable = true;
        }
    }

    void FindAvailableGamesCallback(string[] gameSessionIds, string[] creatorIds, bool[] isPrivate)
    {
        for(int i=0;i<gameSessionIds.Length; i++)
        {
            GameObject match = Instantiate(gameSessionUI, newgameSessionUI.transform.parent);
            match.GetComponentsInChildren<Text>()[1].text = creatorIds[i];
            match.GetComponentsInChildren<Text>()[2].text = isPrivate[i] ? "Private" : "Public";
            match.GetComponentInChildren<InputField>(true).gameObject.SetActive(isPrivate[i]);
            match.GetComponentInChildren<Button>().onClick.AddListener(JoinGame(gameSessionIds[i], i+1));
        }
    }

    void NewGame()
    {
        DeactivateButtons();
        bool isPrivate = newgameSessionUI.GetComponentInChildren<Dropdown>().value == 1;
        string password = newgameSessionUI.GetComponentInChildren<InputField>(true).text;
        if (GameConstants.USE_SERVER)
        {
            StartCoroutine(GameClient.SendCreateGameRequest(isPrivate, password, () => SceneManager.LoadScene("Setup")));
        } else
        {
            SceneManager.LoadScene("Setup");
        }
    }

    UnityAction JoinGame(string gameSessionId, int i)
    {
        return () =>
        {
            DeactivateButtons();
            string pass = newgameSessionUI.transform.parent.GetChild(i).GetComponentInChildren<InputField>(true).text;
            StartCoroutine(GameClient.SendJoinGameRequest(gameSessionId, pass,
                () => SceneManager.LoadScene("Setup"))
            );
        };
    }

    void DeactivateButtons()
    {
        foreach (Button b in newgameSessionUI.transform.parent.GetComponentsInChildren<Button>()) b.interactable = false;
    }
}
