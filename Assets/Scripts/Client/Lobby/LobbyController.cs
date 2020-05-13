using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

public class LobbyController : Controller
{
    public Button backButton;
    public NewGameSessionUiController newGameSessionUI;
    public GameSessionUiController gameSessionUI;
    public GameSessionUiController[] gameSessionUIs;
    public SceneReference initialScene;
    public SceneReference setupScene;
    public StatusModalController statusModal;
    public VerticalLayoutGroup matches;

    void Start ()
    {
        AwsLambdaClient.SendFindAvailableGamesRequest(FindAvailableGamesCallback);

        newGameSessionUI.SetUsername(ProfileController.username);
        newGameSessionUI.SetPlayCallback(NewGame);
        backButton.onClick.AddListener(LoadInitial);
    }

    void FindAvailableGamesCallback(string[] gameSessionIds, string[] creatorIds, bool[] isPrivate)
    {
        gameSessionUIs = Enumerable.Range(0, gameSessionIds.Count()).ToList().ConvertAll(i => CreateGameSessionUi(gameSessionIds[i], creatorIds[i], isPrivate[i])).ToArray();
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
        AwsLambdaClient.SendCreateGameRequest(isPrivate, ProfileController.username, password, SetupGame);
    }

    void JoinGame(GameSessionUiController match, string gameSessionId)
    {
        DeactivateButtons();
        string password = match.GetPassword();
        AwsLambdaClient.SendJoinGameRequest(gameSessionId, ProfileController.username, password, SetupGame);
    }

    void SetupGame(string playerSessionId, string ipAddress, int port)
    {
        BaseGameManager.InitializeStandard(playerSessionId, ipAddress, port);
        SceneManager.LoadScene(setupScene);
    }

    void DeactivateButtons()
    {
        newGameSessionUI.DeactivateButton();
        gameSessionUIs.ToList().ForEach(g => g.DeactivateButton());
    }

    void LoadInitial()
    {
        SceneManager.LoadScene(initialScene);
    }
}
