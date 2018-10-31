using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour {

    public Button backButton;
    public NewGameSessionUiController newGameSessionUI;
    public GameSessionUiController gameSessionUI;
    public GameSessionUiController[] gameSessionUIs;
    public StatusModalController statusModal;
    public VerticalLayoutGroup matches;

    void Start ()
    {
        StartCoroutine(AwsLambdaClient.SendFindAvailableGamesRequest(FindAvailableGamesCallback));

        newGameSessionUI.SetUsername(GameClient.username);
        newGameSessionUI.SetPlayCallback(NewGame);
        backButton.onClick.AddListener(LoadInitial);
    }

    void FindAvailableGamesCallback(string[] gameSessionIds, string[] creatorIds, bool[] isPrivate)
    {
        gameSessionUIs = Util.Map(gameSessionIds, creatorIds, isPrivate, CreateGameSessionUi);
    }

    GameSessionUiController CreateGameSessionUi(string gameSessionId, string creatorId, bool isPrivate)
    {
        GameSessionUiController match = Instantiate(gameSessionUI, matches.transform);
        match.SetUsername(creatorId);
        match.SetPrivacy(isPrivate);
        match.SetPlayCallback(() => JoinGame(match, gameSessionId));
        return match;
    }

    void NewGame()
    {
        DeactivateButtons();
        bool isPrivate = newGameSessionUI.GetPrivacy();
        string password = newGameSessionUI.GetPassword();
        StartCoroutine(AwsLambdaClient.SendCreateGameRequest(isPrivate, GameClient.username, password, SetupNewGame));
    }

    void JoinGame(GameSessionUiController match, string gameSessionId)
    {
        DeactivateButtons();
        string password = match.GetPassword();
        StartCoroutine(AwsLambdaClient.SendJoinGameRequest(gameSessionId, GameClient.username, password, SetupJoinGame));
    }

    void SetupNewGame(string playerSessionId, string ipAddress, int port)
    {
        SetupGame(playerSessionId, ipAddress, port, true);
    }

    void SetupJoinGame(string playerSessionId, string ipAddress, int port)
    {
        SetupGame(playerSessionId, ipAddress, port, false);
    }

    void SetupGame(string playerSessionId, string ipAddress, int port, bool isPrimary)
    {
        BaseGameManager.InitializeStandard(playerSessionId, ipAddress, port, isPrimary);
        SceneManager.LoadScene("Setup");
    }

    void DeactivateButtons()
    {
        newGameSessionUI.DeactivateButton();
        Util.ForEach(gameSessionUIs, g => g.DeactivateButton());
    }

    void LoadInitial()
    {
        SceneManager.LoadScene("Initial");
    }
}
