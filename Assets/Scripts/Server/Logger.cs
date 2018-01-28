using Aws.GameLift;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

class Logger {

    public static void ServerLog(object value)
    {
        if (Application.isEditor)
        {
            Debug.Log("SERVER: " + value);
        }
        else
        {
            Console.WriteLine(value);
        }
    }

    public static void OutcomeError(GenericOutcome outcome)
    {
        ServerLog(outcome.Error.ErrorName + " - " + outcome.Error.ErrorMessage);
    }

    public static void CallbackLog(NetworkMessage netMsg, string msg)
    {
        int cid = -1;
        if (netMsg.conn != null)
        {
            cid = netMsg.conn.connectionId;
        }
        ServerLog(msg + ": " + cid);
    }
    
    public static void ClientLog(object value)
    {
        Debug.Log("CLIENT: " + value);
    }
}
