using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using WarshopCommon;

public class AwsLambdaClient
{
    public const string GATEWAY_URL = "https://l1o387pdnb.execute-api.us-east-1.amazonaws.com/production";
    public static void SendCreateGameRequest(bool isPriv, string username, string pass, UnityAction<string, string, int> callback)
    {
        Messages.CreateGameRequest request = new Messages.CreateGameRequest
        {
            playerId = username,
            isPrivate = isPriv,
            password = isPriv ? pass : "NONE"
        };
        UnityWebRequest www = UnityWebRequest.Put(GATEWAY_URL + "/games", JsonUtility.ToJson(request));
        www.SendWebRequest().completed += (op) => CreateGameResponse(www, callback);
    }

    private static void CreateGameResponse(UnityWebRequest www, UnityAction<string, string, int> callback)
    {
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogError("Error creating new game: \n" + www.downloadHandler.text);
        }
        else
        {
            Messages.CreateGameResponse res = JsonUtility.FromJson<Messages.CreateGameResponse>(www.downloadHandler.text);
            callback(res.playerSessionId, res.ipAddress, res.port);
        }
        www.Dispose();
    }

    public static void SendJoinGameRequest(string gId, string username, string pass, UnityAction<string, string, int> callback)
    {
        Messages.JoinGameRequest request = new Messages.JoinGameRequest
        {
            playerId = username,
            gameSessionId = gId,
            password = pass
        };
        UnityWebRequest www = UnityWebRequest.Put(GATEWAY_URL + "/games", JsonUtility.ToJson(request));
        www.method = "POST"; //LOL you freaking suck Unity
        www.SendWebRequest().completed += (op) => JoinGameResponse(www, callback);
    }

    private static void JoinGameResponse(UnityWebRequest www, UnityAction<string, string, int> callback)
    {
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogError("Error joining game: \n" + www.downloadHandler.text);
        }
        else
        {
            Messages.JoinGameResponse res = JsonUtility.FromJson<Messages.JoinGameResponse>(www.downloadHandler.text);
            callback(res.playerSessionId, res.ipAddress, res.port);
        }
        www.Dispose();
    }

    public static void SendFindAvailableGamesRequest(UnityAction<string[], string[], bool[]> callback)
    {
        UnityWebRequest www = UnityWebRequest.Get(GATEWAY_URL + "/games");
        www.SendWebRequest().completed += (op) => FindAvailableGamesResponse(www, callback);
    }

    private static void FindAvailableGamesResponse(UnityWebRequest www, UnityAction<string[], string[], bool[]> callback)
    {
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.LogError("Error finding available games: \n" + www.downloadHandler.text);
        }
        else
        {
            Messages.GetGamesResponse res = JsonUtility.FromJson<Messages.GetGamesResponse[]>(www.downloadHandler.text);
            callback(res.gameSessionIds, res.creatorIds, res.isPrivate);
        }
        www.Dispose();
    }
}
