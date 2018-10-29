using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class AwsLambdaClient
{
    private static Logger log = new Logger(typeof(AwsLambdaClient));

    public static IEnumerator SendCreateGameRequest(bool isPriv, string username, string pass, UnityAction<string, string, int> callback)
    {
        Messages.CreateGameRequest request = new Messages.CreateGameRequest
        {
            playerId = username,
            isPrivate = isPriv,
            password = isPriv ? pass : "NONE"
        };
        UnityWebRequest www = UnityWebRequest.Put(GameConstants.GATEWAY_URL + "/games", JsonUtility.ToJson(request));
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            log.Fatal("Error creating new game: \n" + www.downloadHandler.text);
        }
        else
        {
            Messages.CreateGameResponse res = JsonUtility.FromJson<Messages.CreateGameResponse>(www.downloadHandler.text);
            if (res.IsError)
            {
                BaseGameManager.ErrorString = res.ErrorMessage;
            }
            else
            {
                callback(res.playerSessionId, res.ipAddress, res.port);
            }
        }
    }

    public static IEnumerator SendJoinGameRequest(string gId, string username, string pass, UnityAction<string, string, int> callback)
    {
        Messages.JoinGameRequest request = new Messages.JoinGameRequest
        {
            playerId = username,
            gameSessionId = gId,
            password = pass
        };
        UnityWebRequest www = UnityWebRequest.Put(GameConstants.GATEWAY_URL + "/games", JsonUtility.ToJson(request));
        www.method = "POST"; //LOL you freaking suck Unity
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            log.Fatal("Error joining available game: \n" + www.downloadHandler.text);
        }
        else
        {
            Messages.JoinGameResponse res = JsonUtility.FromJson<Messages.JoinGameResponse>(www.downloadHandler.text);
            if (res.IsError)
            {
                BaseGameManager.ErrorString = res.ErrorMessage;
            }
            else
            {
                callback(res.playerSessionId, res.ipAddress, res.port);
            }
        }
    }

    public static IEnumerator SendFindAvailableGamesRequest(UnityAction<string[], string[], bool[]> callback)
    {
        UnityWebRequest www = UnityWebRequest.Get(GameConstants.GATEWAY_URL + "/games");
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            log.Fatal("Error finding available games: \n" + www.downloadHandler.text);
        }
        else
        {
            Messages.GetGamesResponse res = JsonUtility.FromJson<Messages.GetGamesResponse>(www.downloadHandler.text);
            if (res.IsError)
            {
                BaseGameManager.ErrorString = res.ErrorMessage;
            }
            else
            {
                callback(res.gameSessionIds, res.creatorIds, res.isPrivate);
            }
        }
    }
}
