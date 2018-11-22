using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyController : Controller
{
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
        StartCoroutine(AwsLambdaClient.SendCreateGameRequest(isPrivate, GameClient.username, password, SetupGame));
    }

    void JoinGame(GameSessionUiController match, string gameSessionId)
    {
        DeactivateButtons();
        string password = match.GetPassword();
        StartCoroutine(AwsLambdaClient.SendJoinGameRequest(gameSessionId, GameClient.username, password, SetupGame));
    }

    void SetupGame(string playerSessionId, string ipAddress, int port)
    {
        BaseGameManager.InitializeStandard(playerSessionId, ipAddress, port);
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
